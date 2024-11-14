using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static SpdtpMessage;
using static SpdtpNegotiationMessage;

/**
* The communication peer
*/
public class SpdtpCliPeer : SpdtpConnection
{
	public bool verbose = false;

	protected SpdtpNegotiationMessage pendingNegotiationMessage;
	protected int resendAttempts = 0;

	protected bool _receiveInterrupt;
	protected int _testingResponseErrorCount = 0;

	public SpdtpCliPeer(IPEndPoint localSocket, IPEndPoint remoteSocket) : base(localSocket, remoteSocket)
	{
		keepAlive = new AsyncTimer(handleKeepAlive, 30000).start();
	}

	protected void sendLoop()
	{
		Console.WriteLine("Your connection is pending!\nType 'open' to open the communication session!\nType # something to send a textual message!");
		while (isRunning)
		{
			String userInput = Console.ReadLine();
			if (userInput.Length > 2 && userInput.StartsWith("-er"))
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
			else if (userInput.StartsWith("-verbo"))
			{
				Console.WriteLine(verbose ^= true);
			}
			else if (userInput.StartsWith("-interr"))
			{
				if (_receiveInterrupt ^= true)
					keepAlive.stop();
				else
					keepAlive.start();
				Console.WriteLine(_receiveInterrupt);
			}
			else if (userInput.StartsWith("#")) // Temp
			{
				byte[] data = Encoding.UTF8.GetBytes(userInput);
				udpClient.Send(data, data.Length, remoteSocket);
			}
			else if (userInput.StartsWith("disc"))
			{
				doTerminate();
				break;
			}
			else if (userInput.StartsWith("op"))
			{
				try 
				{
					String[] args = userInput.Split(" ");

					short segmentPayloadSize;
					if (args.Length > 1)
						segmentPayloadSize = short.Parse(args[1]);
					else
					{
						Console.WriteLine("Specify the size of segment's payload in bytes that will be used by this session: ");
						segmentPayloadSize = short.Parse(Console.ReadLine());
					}

					pendingNegotiationMessage = sendMessage(new SpdtpNegotiationMessage((byte) (NEGOTIATION | STATE_REQUEST), segmentPayloadSize), args.Length > 2 && args[2] == "-e");
					keepAlive.restart();

					if (session == null)
					{
						session = new Session(segmentPayloadSize);
						Console.WriteLine("Session with segment's payload size of " + segmentPayloadSize + " was initiated!");
					}
					else
					{
						session.setSegmentPayloadSize(segmentPayloadSize);
						Console.WriteLine("Session's segment's payload size was updated to " + segmentPayloadSize + "!");
					}
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("Error has occurred: " + ex);
				}
			}
		}
	}

	protected override T sendMessage<T>(T message, bool err = false)
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
				if (_receiveInterrupt)
					continue;

				byte[] rawMsg = udpClient.Receive(ref localSocket);
				if (rawMsg[0] == '#') // Temp
				{
					Console.WriteLine("Message received: " + Encoding.UTF8.GetString(rawMsg).Substring(1));
					continue;
				}

				SpdtpMessage spdtpMessage = newMessageFromBytes(rawMsg);
				keepAlive.restart();

				if (verbose)
					Console.WriteLine("Message received:  " + spdtpMessage + " - " + Utils.formatHeader(rawMsg));

				if (spdtpMessage is SpdtpNegotiationMessage)
				{
					SpdtpNegotiationMessage negotiationMessage = (SpdtpNegotiationMessage) spdtpMessage;

					if (handleNegotiationMsg(negotiationMessage))
						continue;
				}

				Console.WriteLine("Unknown message was received: " + spdtpMessage);
			}
			catch (SocketException ex)
			{
				if (isRunning)
				{
					Console.Error.WriteLine("SocketException (" + ex.ErrorCode + "): " + ex.Message);
					if (ex.ErrorCode == 10054) // Other peer died...
						close();
				}
			}
		}
	}

	protected bool handleResend(SpdtpNegotiationMessage negotiationMessage)
	{
		if (pendingNegotiationMessage != null)
		{
			// Console.WriteLine(resendAttempts);
			if (resendAttempts++ > 2)
			{
				doTerminate("Session and connection terminated (too many transmission errors)!");
				return false;
			}

			sendMessageAsync(pendingNegotiationMessage);
			return true;
		}
		else
			return false;
	}

	protected bool handleNegotiationMsg(SpdtpNegotiationMessage negotiationMessage)
	{
		if (negotiationMessage.getMessageFlags() == SESSION_TERMINATION_8x1)
		{
			Console.WriteLine("Session and connection was terminated by the other peer!");

			close();
			return true;
		}

		keepAlive.setTimeout(5000);

		if (!negotiationMessage.validate())
		{
			Console.WriteLine("Erroneous negotiation message was received: " + negotiationMessage + "!");

			if (!handleResend(negotiationMessage))
			{
				sendMessageAsync(negotiationMessage.createResendRequest());
				Console.WriteLine("Requesting resend!");
			}
			else
				Console.WriteLine("Resend performed!");
			return true;
		}

		if (negotiationMessage.isState(STATE_REQUEST))
		{
			if (session == null)
			{
				session = new Session(negotiationMessage.getSegmentPayloadSize());
				if (negotiationMessage.getKeepAliveFlag() == 0)
					Console.WriteLine("Session with segment's payload size of " + negotiationMessage.getSegmentPayloadSize() + " bytes was established with the other peer!");
			}
			else
			{
				session.setSegmentPayloadSize(negotiationMessage.getSegmentPayloadSize());
				if (negotiationMessage.getKeepAliveFlag() == 0)
					Console.WriteLine("Session's segment's payload size was updated to " + negotiationMessage.getSegmentPayloadSize() + " bytes by the other peer!");
			}

			sendMessageAsync(negotiationMessage.createResponse(), _testingResponseErrorCount-- > 0);
			// Console.WriteLine(msg);
			return true;
		}

		if (negotiationMessage.isState(STATE_RESPONSE))
		{
			if (session != null && negotiationMessage.getSegmentPayloadSize() != session.getSegmentPayloadSize())
			{
				session.setSegmentPayloadSize(negotiationMessage.getSegmentPayloadSize());
				if (negotiationMessage.getKeepAliveFlag() == 0)
					Console.WriteLine("Session's segment's payload size was updated by the other peer to " + negotiationMessage.getSegmentPayloadSize() + "!");
			}

			pendingNegotiationMessage = null;
			resendAttempts = 0;
			return true;
		}

		if (negotiationMessage.isState(STATE_RESEND_REQUEST) && pendingNegotiationMessage != null)
		{
			if (!handleResend(negotiationMessage))
				Console.WriteLine("Nothing to resend...");
			return true;
		}

		return false;
	}

	public override void handleKeepAlive()
	{
		// Console.WriteLine("Keep alive" + keepAlive.getTimeoutCount());
		if (keepAlive.getTimeoutCount() > 3)
			doTerminate("Session and connection terminated (timeout)!");
		else if (session != null)
		{
			pendingNegotiationMessage = sendMessage(new SpdtpNegotiationMessage((byte) (NEGOTIATION | KEEP_ALIVE | STATE_REQUEST), session.getSegmentPayloadSize()));
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