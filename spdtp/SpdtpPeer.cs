using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static SpdtpMessage;

class SpdtpPeer
{
	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning = true;

	protected Session session;

	public SpdtpPeer(IPEndPoint localSocket, IPEndPoint remoteSocket)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);
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
				Console.WriteLine("Connection terminated!");
				isRunning = false;
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

					session = new Session(0);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine("Error has occurred: " + ex);
				}
			}
		}
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

				SpdtpMessage spdtpMessage = SpdtpMessage.newMessageFromBytes(rawMsg);

				if (spdtpMessage is SpdtpNegotiationMessage)
				{
					SpdtpNegotiationMessage negotiationMessage = (SpdtpNegotiationMessage) spdtpMessage;

					if (negotiationMessage.isState(STATE_REQUEST))
					{
						Console.WriteLine("Session with segment's payload size of " + negotiationMessage.getSegmentPayloadSize() + " bytes was opened!");

						if (session == null)
							session = new Session(negotiationMessage.getSegmentPayloadSize());
						else
							session.setSegmentPayloadSize(negotiationMessage.getSegmentPayloadSize());

						byte[] negotiationResponse = negotiationMessage.createResponse().getBytes();
						udpClient.Send(negotiationResponse, negotiationResponse.Length, remoteSocket);
						// Console.WriteLine(msg);
						continue;
					}
		
					if (negotiationMessage.isState(STATE_RESPONSE))
					{
						Console.WriteLine("Session's segment's payload size was updated to " + negotiationMessage.getSegmentPayloadSize() + "!");
						session.setSegmentPayloadSize(negotiationMessage.getSegmentPayloadSize());

						continue;
					}
				}

				Console.WriteLine("Unknown message was received: " + spdtpMessage);
			}
			catch (SocketException ex)
			{
				Console.Error.WriteLine("SocketException:" + ex.Message);
				isRunning = false;
			}
		}
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

	public void stop()
	{
		isRunning = false;
		udpClient.Close();
	}
}