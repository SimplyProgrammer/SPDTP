using System;
using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static MessageBase;
using static ResourceInfoMessage;
using static NegotiationMessage;

/**
* The implementation Spdtp peer with CLI interface as the implementor of Connection...
*/
public class CliPeer : Connection
{
	public bool verbose = false;

	protected NegotiationMessage pendingNegotiationMessage;
	protected int resendErrAttempts = 0;
	protected int standardKeepAlivePeriod = 5000;

	protected bool _receiveInterrupt;
	internal int _testingErrorCount = 0, _everyNthError = 0, _requestsBeforeErr = 0;

	protected String saveDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "downloads");
	// protected int _interruptedLatency = 0;

	public CliPeer(IPEndPoint localSocket, IPEndPoint remoteSocket) : base(localSocket, remoteSocket, 30000)
	{
		keepAlive.start();
	}

	protected String getHelp(bool v = false)
	{
		String str = "Connection " + this + " is established!\n";
		if (session != null)
		{
			str += "Session was opened with segment's payload size of " + session.getMetadata().getSegmentPayloadSize() + " bytes!\n";
			str += "Type 'open' to change the segment's payload size of the session!\n";
			str += "Type #<message> something to send a textual message!\nType #!<valid file path> to send file!\n";
			str += "Type 'disc' to terminate the session and connection!\n";
			str += "Type 'save-dir <valid directory path>' to specify where to save received files!\n";

			if (v || verbose)
			{
				str += "Transmissions: {\n";

				foreach (var entry in session.getTransmissions())
				{
					str += entry.Key + ": " + entry.Value.ToString() + "\n";
				}
				str += "}\n\n";

				str += "-er <faulty message count> <interval>: Simulate errors (1 bit flip). Sets the count of erroneous outgoing messages and interval between them.\n";
				str += "-kpal-per <milliseconds>: Update the keep-alive period.\n";
				str += "-kpal-rest: Restart the keep-alive timer immediately.\n";
				str += "-cls-res: Clear all active resource transmissions.\n";
				str += "-verbo: Toggle verbose logging on/off.\n";
				str += "-interr <delay in ms>: Toggle simulated interruption in receiving messages after specified delay.\n";
			}
		}
		else
		{
			str += "Type 'open' to open the communication session with specified segment's payload size!\n";
			str += "Type 'disc' to terminate the session and connection!\n";
			str += "Type '?' or '? -v' to see help...\n";
		}

		return str;
	}

	protected void sendLoop()
	{
		Console.WriteLine(getHelp());
		while (isRunning)
		{
			try 
			{
				String userInput = Console.ReadLine();
				if (userInput.Length < 1)
					continue;

				if (userInput.StartsWith("?")) // CLI code...
				{
					Console.WriteLine(getHelp(userInput.EndsWith("-v")));
				}
				else if (userInput.Length > 2 && userInput.StartsWith("-er"))
				{
					try
					{
						String[] args = userInput.Substring(3).Trim().Split(' ');

						if (args.Length > 0)
							_testingErrorCount = int.Parse(args[0]);
						
						if (args.Length > 1)
							_everyNthError = int.Parse(args[1]);
						else
							_everyNthError = 0;
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine("Error has occurred: " + ex.Message);
					}

					_requestsBeforeErr = 0;
					Console.WriteLine(_testingErrorCount + " " + _everyNthError);
				}
				else if (userInput.StartsWith("-kpal-per"))
				{
					String[] args = userInput.Split(' ');

					int newKpAlivePeriod = standardKeepAlivePeriod;
					try
					{
						newKpAlivePeriod = int.Parse(args[1]);
						if (newKpAlivePeriod < 1)
							newKpAlivePeriod = standardKeepAlivePeriod;
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine("Error has occurred: " + ex.Message);
					}

					Console.WriteLine(newKpAlivePeriod);
					keepAlive.setTimeout(standardKeepAlivePeriod = newKpAlivePeriod);
				}
				else if (userInput.StartsWith("-kpal-rest"))
				{
					keepAlive.restart();
				}
				else if (userInput.StartsWith("-cls-res"))
				{
					session.getTransmissions().Clear();
				}
				else if (userInput.StartsWith("-verbo"))
				{
					Console.WriteLine(verbose ^= true);
				}
				else if (userInput.StartsWith("-interr"))
				{
					new Thread(() => {
						try
						{
							int _interruptedLatency = 0;
							String[] args = userInput.Split(' ');
							if (args.Length > 1)
								_interruptedLatency = int.Parse(args[1]);
							Thread.Sleep(_interruptedLatency);
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine("Error has occurred: " + ex.Message);
						}

						if (_receiveInterrupt ^= true)
							keepAlive.stop();
						else
							keepAlive.start();
						Console.WriteLine(_receiveInterrupt);
					}) { IsBackground = true }.Start();
				}

				else if (userInput.StartsWith("#")) // Temp
				{
					if (session == null)
					{
						Console.WriteLine("Session is was not opened (null)!");
						continue;
					}

					if (userInput.Length > 1 && userInput[1] == '!')
					{
						if ((userInput = userInput.Substring(2).Trim()).Length < 1)
							continue;

						FileStream resource = File.Open(userInput, FileMode.Open, FileAccess.Read);
						
						byte[] bytes = new byte[resource.Length];
						resource.Read(bytes, 0, bytes.Length);

						session.sendResource(bytes, resource/*, args.Length > 1 && args[1] == "-e"*/);
						resource.Close();
					}
					else
					{
						if ((userInput = userInput.Substring(1).Trim()).Length < 1)
							continue;

						byte[] bytes = Encoding.ASCII.GetBytes(userInput);
						session.sendResource(bytes, userInput/*, args.Length > 1 && args[1] == "-e"*/);
					}

					keepAlive.restart();
				}
				else if (userInput.StartsWith("disc"))
				{
					doTerminate();
					break;
				}
				else if (userInput.StartsWith("op"))
				{
					String[] args = userInput.Split(' ');

					short segmentPayloadSize;
					if (args.Length > 1)
						segmentPayloadSize = short.Parse(args[1]);
					else
					{
						Console.WriteLine("Specify the size of segment's payload in bytes that will be used by this session: ");
						segmentPayloadSize = short.Parse(Console.ReadLine());
					}

					if (segmentPayloadSize < 1)
					{
						Console.WriteLine("Segment's payload size must be at least 1 byte (recommended: 10+)!");
						continue;
					}

					if (args.Length > 2 && args[2] == "-e")
						_testingErrorCount++;
					if (segmentPayloadSize < 10)
						Console.WriteLine("Warning: Segment's payload size is too small for any reasonable communication. Consider setting it to at least 10 bytes!");
					else if (segmentPayloadSize > MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE)
					{
						Console.WriteLine("Warning: Segment's payload size is too big and lower layer fragmentation can be expected! Setting it to " + MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE + "!");
						segmentPayloadSize = MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE;
					}

					pendingNegotiationMessage = openSession(segmentPayloadSize, standardKeepAlivePeriod);
				}
				else if (userInput.StartsWith("save-dir"))
				{
					String[] args = userInput.Split(' ');
					
					if (args.Length > 1)
					{
						String pathToSave = args[1];
						if (pathToSave.Length < 1)
							pathToSave = saveDirectory;

						try
						{
							Directory.CreateDirectory(Path.GetDirectoryName(pathToSave));
						}
						catch (Exception ex)
						{
							pathToSave = Path.Combine(Environment.ExpandEnvironmentVariables("%userprofile%"), "downloads");
							Console.WriteLine("There is no such a directory... Defaulting to:");
						}

						saveDirectory = pathToSave;
					}

					Console.WriteLine(saveDirectory);
				}
				else
					Console.WriteLine("Unknown! Please type '?' or '? -v'");
			}
			catch (FormatException ex)
			{
				Console.Error.WriteLine("Error has occurred: " + ex.Message);
			}
			catch (IOException ex)
			{
				Console.Error.WriteLine("Error has occurred: " + ex.Message);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error has occurred: " + ex);
			}
		}
	}

	public override void handleTransmittedResource(ResourceTransmission finishedResourceTransmission)
	{
		byte[] bytes = finishedResourceTransmission.reconstructResource();
		var time = finishedResourceTransmission.getBenchmarkTimer().ElapsedMilliseconds;
		if (finishedResourceTransmission.getMetadata().getResourceName().StartsWith(TEXT_MSG_MARK, StringComparison.Ordinal))
		{
			Console.WriteLine("\n---------- Text message ----------\n" + 
							"Received segment count: " + finishedResourceTransmission.getProcessedSegmentCount() + "\n" +
							bytes.Length + " bytes / " + time + " ms!\n" +
							remoteSocket + ":");

			String textMessage = Encoding.ASCII.GetString(bytes);
			textMessage = Program.cypher(textMessage);
			Console.WriteLine(textMessage);

			int pairCount = 0;
			for (int i = 0; i < textMessage.Length; i++)
			{
				if (textMessage[i] == ' ')
					pairCount++;
			}
			Console.WriteLine("Pair count: " + pairCount);

			return;
		}
		
		Console.WriteLine("\n---------- Incoming file ---------- \n" + 
							"Received segment count: " + finishedResourceTransmission.getProcessedSegmentCount() + "\n" +
							bytes.Length + " bytes / " + time + " ms!\n" +
							remoteSocket + " => " + finishedResourceTransmission.getMetadata().getResourceName() + ":\n" + 
							"Saving into \"" + saveDirectory + "\"...");

		try
		{
			var fs = new FileStream(Path.Combine(saveDirectory, Path.GetFileName(finishedResourceTransmission.getMetadata().getResourceName())), FileMode.Create, FileAccess.Write);
			fs.Write(bytes, 0, bytes.Length);

			Console.WriteLine("Successfully wrote " + fs.Length + " bytes into " + fs.Name + "!");
			fs.Close();
		}
		catch (Exception ex) // Should not happen
		{
			Console.Error.WriteLine("Error has occurred: " + ex.Message);
		}
	}

	public override T sendMessage<T>(T message)
	{
		byte[] msgBytes = message.getBytes();

		bool wasErr = false;
		if (_testingErrorCount > 0)
		{
			if (++_requestsBeforeErr >= _everyNthError)
			{
				Utils.introduceRandErrors(msgBytes);
				_requestsBeforeErr = 0;
				_testingErrorCount--;

				wasErr = true;
			}
		}

		udpClient.Send(msgBytes, msgBytes.Length, remoteSocket);

		if (verbose)
			Console.WriteLine("Message sent:  " + message + " - " + Utils.formatHeader(msgBytes) + (wasErr ? "(with intentional error)" : ""));
		return message;
	}

	protected override void receiveLoop()
	{
		while (isRunning)
		{
			try
			{
				byte[] rawMsg = udpClient.Receive(ref localSocket);
				if (_receiveInterrupt)
					continue;

				MessageBase spdtpMessage = newMessageFromBytes(rawMsg);
				keepAlive.restart();

				if (verbose)
					Console.WriteLine("Message received:  " + spdtpMessage + " - " + Utils.formatHeader(rawMsg));

				if (spdtpMessage is NegotiationMessage && handleNegotiationMsg((NegotiationMessage) spdtpMessage))
					continue;

				if (session != null && session.handleIncomingMessage(spdtpMessage))
					continue;

				Console.WriteLine("Unknown message was received: " + spdtpMessage + "!");
			}
			catch (SocketException ex)
			{
				if (isRunning)
				{
					if (ex.ErrorCode == 10054) // On ICMP failed. Other peer died...
					{
						Console.WriteLine("Other peer seems to be unreachable!");

						// close();
						continue;
					}
					Console.Error.WriteLine("SocketException (" + ex.ErrorCode + "): " + ex.Message);
				}
			}
		}
	}

	public override bool attemptResend(MessageBase message)
	{
		if (resendErrAttempts++ > ACCEPTABLE_ERR_COUNT)
		{
			doTerminate("Session and connection terminated (too many transmission errors)!");
			return false;
		}
		
		sendMessage(message);
		return true;
	}

	public override void resetResendAttempts(int to = 0)
	{
		resendErrAttempts = to;
	}

	protected bool handleNegotiationMsg(NegotiationMessage negotiationMessage)
	{
		if (negotiationMessage.getMessageFlags() == SESSION_TERMINATION_8x1)
		{
			Console.WriteLine("Session and connection was terminated by the other peer!");

			close();
			return true;
		}

		if (!negotiationMessage.validate())
		{
			Console.WriteLine("Erroneous negotiation message was received: " + negotiationMessage + "!");

			if (pendingNegotiationMessage == null)
			{
				sendMessage(negotiationMessage.createResendRequest(negotiationMessage.getKeepAliveFlag()));
				Console.WriteLine("Resend requested!");
			}
			else if (attemptResend(pendingNegotiationMessage))
				Console.WriteLine("Resend performed!");
			return true;
		}

		if (negotiationMessage.isState(STATE_REQUEST))
		{
			if (session == null)
			{
				session = new Session(this, negotiationMessage);
				Console.WriteLine("Session with segment's payload size of " + negotiationMessage.getSegmentPayloadSize() + " bytes was established with the other peer!");
			}
			else
			{
				if (session.getMetadata().getSegmentPayloadSize() != negotiationMessage.getSegmentPayloadSize())
				{
					session.setMetadata(negotiationMessage);
					Console.WriteLine("Session's segment's payload size was adjusted to " + negotiationMessage.getSegmentPayloadSize() + " bytes by the other peer!");
				}

				session.onKeepAlive();
			}

			keepAlive.setTimeout(standardKeepAlivePeriod);
			sendMessage(negotiationMessage.createResponse(negotiationMessage.getKeepAliveFlag())/*, 0, 0, _testingErrorCount-- > 0*/);
			// Console.WriteLine(msg);
			return true;
		}

		if (negotiationMessage.isState(STATE_RESPONSE))
		{
			if (session != null)
			{
				if (negotiationMessage.getSegmentPayloadSize() != session.getMetadata().getSegmentPayloadSize())
				{
					session.setMetadata(negotiationMessage);
					Console.WriteLine("Session's segment's payload size was updated by the other peer to " + negotiationMessage.getSegmentPayloadSize() + "!");
				}

				session.onKeepAlive();
			}

			pendingNegotiationMessage = null;
			resetResendAttempts();
			return true;
		}

		if (negotiationMessage.isState(STATE_RESEND_REQUEST))
		{
			if (pendingNegotiationMessage == null)
				Console.WriteLine("Nothing to resend...");
			else
				attemptResend(pendingNegotiationMessage);
			return true;
		}

		return false;
	}

	public override void handleKeepAlive(AsyncTimer keepAlive)
	{
		// Console.WriteLine("Keep alive" + keepAlive.getTimeoutCount());
		if (keepAlive.getTimeoutCount() > KEEP_ALIVE_ATTEMPTS)
			doTerminate("Session and connection terminated (timeout)!");
		else if (session != null)
		{
			if (pendingNegotiationMessage != null)
				Console.WriteLine("Keep alive missed by the other peer...");
			pendingNegotiationMessage = sendMessage(session.getMetadata().clone(KEEP_ALIVE));
		}
		else
			Console.WriteLine("Please use 'open' to open the communication session or the connection will be terminated soon!");
	}

	public override void start()
	{
		base.start();

		// Thread sendingThread = new Thread(sendLoop);
		// sendingThread.Start();
		sendLoop();
	}
}