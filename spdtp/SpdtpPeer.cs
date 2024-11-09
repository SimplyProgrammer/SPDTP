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
class SpdtpPeer
{
	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning = true;

	protected Session session;
	protected AsyncTimer keepAlive;

	public SpdtpPeer(IPEndPoint localSocket, IPEndPoint remoteSocket)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);

		keepAlive = new AsyncTimer(handleKeepAlive, 30000 + (localSocket.Port % 2)*1000 /* prevent double keep alive */).start();
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
				terminate();
				break;
			}
			else if (userInput.StartsWith("open"))
			{
				try 
				{
					Console.WriteLine("Specify the size of segment's payload in bytes that will be used by this session: ");
					short segmentPayloadSize = short.Parse(Console.ReadLine());

					byte[] negotiationMessage = new SpdtpNegotiationMessage((byte) (NEGOTIATION | STATE_REQUEST), segmentPayloadSize).getBytes();
					udpClient.Send(negotiationMessage, negotiationMessage.Length, remoteSocket);
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

	protected void terminate(String msg = "Session and connection terminated!")
	{
		Console.WriteLine(msg);
		byte[] negotiationMessage = newSessionTerminationRequest().getBytes();
		udpClient.Send(negotiationMessage, negotiationMessage.Length, remoteSocket);

		close();
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

			byte[] negotiationResponse = negotiationMessage.createResponse().getBytes();
			udpClient.Send(negotiationResponse, negotiationResponse.Length, remoteSocket);
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

			return true;
		}

		return false;
	}

	public void handleKeepAlive()
	{
		// Console.WriteLine("Keep alive");

		byte[] negotiationMessage;
		if (keepAlive.getTimeoutCount() > 1)
			terminate("Session and connection terminated (timeout)!");
		else if (session != null)
		{
			negotiationMessage = new SpdtpNegotiationMessage((byte) (NEGOTIATION | KEEP_ALIVE | STATE_REQUEST), session.getSegmentPayloadSize()).getBytes();
			udpClient.Send(negotiationMessage, negotiationMessage.Length, remoteSocket);
		}
		else
			Console.WriteLine("Please use 'open' to open the communication session or the connection will be terminated in following 30s!");
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