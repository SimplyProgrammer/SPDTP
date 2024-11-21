using System;
using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static SpdtpMessage;
using static SpdtpNegotiationMessage;

/**
* The implementation Spdtp peer with CLI interface as the implementor of Connection...
*/
public class CliPeer : Connection
{
	public static readonly int KEEP_ALIVE_ATTEMPTS = 3;
	public static readonly int ACCEPTABLE_ERR_COUNT = 2;

	public bool verbose = false;

	protected SpdtpNegotiationMessage pendingNegotiationMessage;
	protected int resendErrAttempts = 0;
	protected int standardKeepAlivePeriod = 5000;

	protected bool _receiveInterrupt;
	internal int _testingResponseErrorCount = 0;
	// protected int _interruptedLatency = 0;

	public CliPeer(IPEndPoint localSocket, IPEndPoint remoteSocket) : base(localSocket, remoteSocket, 30000)
	{
		keepAlive.start();
	}

	protected String getHelp()
	{
		String str = "Connection " + this + " is established!\n";
		if (session != null)
		{
			str += "Session was opened with segment's payload size of " + session.getMetadata().getSegmentPayloadSize() + " bytes!\n";
			str += "Type 'open' to change the segment's payload size of the session!\n";
			str += "Type #<message> something to send a textual message!\nType #!<file path> to send file!\n";
			str += "Type 'disc' to terminate the session and connection!\n";

			str += "Transmissions: {\n";

			foreach(KeyValuePair<int, ResourceTransmission> entry in session.getTransmissions())
			{
				str += entry.Key + ": " + entry.Value.ToString() + "\n";
			}

			str += "}";
		}
		else
		{
			str += "Type 'open' to open the communication session with specified segment's payload size!\n";
			str += "Type 'disc' to terminate the session and connection!\n";
			str += "Type ? to see help...\n";
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

				if (userInput.Equals("?"))
				{
					Console.WriteLine(getHelp());
				}
				else if (userInput.Length > 2 && userInput.StartsWith("-er"))
				{
					try
					{
						_testingResponseErrorCount = int.Parse(userInput.Substring(3));
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine("Error has occurred: " + ex);
					}
				}
				else if (userInput.StartsWith("-kpal-per"))
				{
					String[] args = userInput.Split(' ');

					int newKpAlivePeriod = 0;
					try
					{
						newKpAlivePeriod = int.Parse(args[1]);
						if (newKpAlivePeriod < 1)
							newKpAlivePeriod = standardKeepAlivePeriod;
					}
					catch (Exception ex)
					{}

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
					// String[] args = userInput.Split(' ');
					// if (args.Length > 1)
					// 	_interruptedLatency = int.Parse(args[1]);

					if (_receiveInterrupt ^= true)
						keepAlive.stop();
					else
						keepAlive.start();
					Console.WriteLine(_receiveInterrupt);
				}

				else if (userInput.StartsWith("#")) // Temp
				{
					if (session == null)
					{
						Console.WriteLine("Session is was not opened (null)!");
						continue;
					}

					String[] args = userInput.Split(' ');
					if (userInput.Length > 1 && userInput[1] == '!')
					{
						if ((userInput = userInput.Substring(2).Trim()).Length < 1)
							continue;

						FileStream resource = File.Open(userInput, FileMode.Open, FileAccess.Read);
						
						byte[] bytes = new byte[resource.Length];
						resource.Read(bytes, 0, bytes.Length);
						resource.Close();

						session.sendResource(bytes, resource, args.Length > 1 && args[1] == "-e");
					}
					else
					{
						if ((userInput = userInput.Substring(1).Trim()).Length < 1)
							continue;

						byte[] bytes = Encoding.ASCII.GetBytes(userInput);
						session.sendResource(bytes, userInput, args.Length > 1 && args[1] == "-e");
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

					pendingNegotiationMessage = openSession(segmentPayloadSize, standardKeepAlivePeriod, args.Length > 2 && args[2] == "-e");
					if (segmentPayloadSize < 10)
						Console.WriteLine("Warning: Segment's payload size is too small for any reasonable communication. Consider setting it to at least 10 bytes!");
					else if (segmentPayloadSize > MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE)
						Console.WriteLine("Warning: Segment's payload size is too big and lower layer fragmentation can be expected, reducing the performance! Consider setting it to no more than " + MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE + "!");
				}
				else
					Console.WriteLine("Unknown! Please type  ?");
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error has occurred: " + ex);
			}
		}
	}

	public override void handleTransmittedResource(ResourceTransmission finishedResourceTransmission)
	{
		// TODO
	}

	public override T sendMessage<T>(T message, bool err = false)
	{
		byte[] msgBytes = message.getBytes();
		if (err)
			Utils.introduceRandErrors(msgBytes);
		udpClient.Send(msgBytes, msgBytes.Length, remoteSocket);

		if (verbose)
			Console.WriteLine("Message sent:  " + message + " - " + Utils.formatHeader(msgBytes) + (err ? "(with intentional error)" : ""));
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

				SpdtpMessage spdtpMessage = newMessageFromBytes(rawMsg);
				keepAlive.restart();

				if (verbose)
					Console.WriteLine("Message received:  " + spdtpMessage + " - " + Utils.formatHeader(rawMsg));

				if (spdtpMessage is SpdtpNegotiationMessage)
				{
					if (handleNegotiationMsg((SpdtpNegotiationMessage) spdtpMessage))
						continue;
				}

				if (session != null)
				{
					if (spdtpMessage is SpdtpResourceInfoMessage)
					{
						if (session.handleIncomingResourceMsg((SpdtpResourceInfoMessage) spdtpMessage))
							continue;
					}

					if (spdtpMessage is SpdtpResourceSegment)
					{
						if (session.handleResourceSegmentMsg((SpdtpResourceSegment) spdtpMessage))
							continue;
					}
				}

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

	public override bool attemptResend(SpdtpMessage message)
	{
		if (resendErrAttempts++ > ACCEPTABLE_ERR_COUNT)
		{
			doTerminate("Session and connection terminated (too many transmission errors)!");
			return false;
		}
		
		sendMessageAsync(message);
		return true;
	}

	public override void resetResendAttempts(int to = 0)
	{
		resendErrAttempts = to;
	}

	protected bool handleNegotiationMsg(SpdtpNegotiationMessage negotiationMessage)
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
				sendMessageAsync(negotiationMessage.createResendRequest());
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
			else if (session.getMetadata().getSegmentPayloadSize() != negotiationMessage.getSegmentPayloadSize())
			{
				session.setMetadata(negotiationMessage);
				Console.WriteLine("Session's segment's payload size was adjusted to " + negotiationMessage.getSegmentPayloadSize() + " bytes by the other peer!");
			}

			keepAlive.setTimeout(standardKeepAlivePeriod);
			sendMessageAsync(negotiationMessage.createResponse(), 0, 0, _testingResponseErrorCount-- > 0);
			// Console.WriteLine(msg);
			return true;
		}

		if (negotiationMessage.isState(STATE_RESPONSE))
		{
			if (session != null && negotiationMessage.getSegmentPayloadSize() != session.getMetadata().getSegmentPayloadSize())
			{
				session.setMetadata(negotiationMessage);
				Console.WriteLine("Session's segment's payload size was updated by the other peer to " + negotiationMessage.getSegmentPayloadSize() + "!");
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
			pendingNegotiationMessage = sendMessage(session.getMetadata());
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