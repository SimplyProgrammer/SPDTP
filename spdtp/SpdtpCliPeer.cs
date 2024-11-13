using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static SpdtpMessage;
using static SpdtpNegotiationMessage;

/**
* The communication peer
*/
class SpdtpCliPeer
{
	public bool verbose = false;

	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning;

	protected Session session;
	protected AsyncTimer keepAlive;
	protected SpdtpNegotiationMessage pendingNegotiationMessage;

	public SpdtpCliPeer(IPEndPoint localSocket, IPEndPoint remoteSocket)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);

		keepAlive = new AsyncTimer(handleKeepAlive, 30000).start();
	}

	protected void sendLoop()
	{
		Console.WriteLine("Your connection is pending!\nType 'open' to open the communication session!\nType # something to send a textual message!");
		while (isRunning)
		{
			String userInput = Console.ReadLine();
			if (userInput.StartsWith("#")) // Temp
			{
				byte[] data = Encoding.UTF8.GetBytes(userInput);
				udpClient.Send(data, data.Length, remoteSocket);
			}
			else if (userInput.StartsWith("disc"))
			{
				doTerminate();
				break;
			}
			else if (userInput.StartsWith("open"))
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
					keepAlive.setTimeout(5000);
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

					session = new Session(segmentPayloadSize);

				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("Error has occurred: " + ex);
				}
			}
		}
	}

	protected void doTerminate(String msg = "Session and connection terminated!")
	{
		Console.WriteLine(msg);
		sendMessage(newSessionTerminationRequest());

		close();
	}

	protected T sendMessage<T>(T message, bool err = false) where T : SpdtpMessage
	{
		byte[] msgBytes = message.getBytes();
		if (err)
			Utils.introduceRandErrors(msgBytes);
		udpClient.Send(msgBytes, msgBytes.Length, remoteSocket);

		if (verbose)
			Console.WriteLine("Message sent:  " + message + " - " + Utils.formatHeader(msgBytes) + (err ? "(with intentional error)" : ""));
		return message;
	}

	protected void receiveLoop()
	{
		while (isRunning)
		{
			try
			{
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
			Console.WriteLine("Erroneous negotiation message was received: " + negotiationMessage + "! Requesting resend...");

			sendMessage(negotiationMessage.createResendRequest());
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

			sendMessage(negotiationMessage.createResponse());
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
			return true;
		}

		if (negotiationMessage.isState(STATE_RESEND_REQUEST) && pendingNegotiationMessage != null)
		{
			sendMessage(pendingNegotiationMessage);
			return true;
		}

		return false;
	}

	public void handleKeepAlive()
	{
		// Console.WriteLine("Keep alive");

		if (keepAlive.getTimeoutCount() > 2)
			doTerminate("Session and connection terminated (timeout)!");
		else if (session != null)
		{
			pendingNegotiationMessage = sendMessage(new SpdtpNegotiationMessage((byte) (NEGOTIATION | KEEP_ALIVE | STATE_REQUEST), session.getSegmentPayloadSize()));
		}
		else
			Console.WriteLine("Please use 'open' to open the communication session or the connection will be terminated soon!");
	}

	public void start()
	{
		isRunning = true;

		Thread receiveThread = new Thread(receiveLoop);
		receiveThread.Start();

		// Thread sendingThread = new Thread(sendLoop);
		// sendingThread.Start();
		sendLoop();
	}

	public void close(int delay = 0)
	{
		if (delay > 0)
			Thread.Sleep(delay);

		isRunning = false;
		udpClient.Close();
		keepAlive.stop();
	}
}