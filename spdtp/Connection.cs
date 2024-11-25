using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static MessageBase;
using static NegotiationMessage;

/**
* The abstraction of UDP Connection (peer)
*/
public abstract class Connection
{
	public static readonly int KEEP_ALIVE_ATTEMPTS = 3;
	public static readonly int ACCEPTABLE_ERR_COUNT = 2;

	public static readonly short MAX_RECOMMENDED_SEGMENT_PAYLOAD_SIZE = 1460; // = 1500 - 20 (ip header) - 8 (Udp) - 12 (Resource segment header, biggest one)

	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning;

	protected Session session;
	protected AsyncTimer keepAlive;

	public Connection(IPEndPoint localSocket, IPEndPoint remoteSocket, int keepAlivePeriod)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);

		keepAlive = new AsyncTimer(handleKeepAlive, keepAlivePeriod);

		udpClient.Client.ReceiveBufferSize = 524288000;
		// udpClient.Client.SendBufferSize = 2169826;

		// Console.WriteLine(udpClient.Client.ReceiveBufferSize + ", " + udpClient.Client.SendBufferSize);
	}

	public override string ToString()
	{
		return GetType().Name + "[" + localSocket.ToString() + " <-> " + remoteSocket.ToString() + "]";
	}

	public virtual NegotiationMessage openSession(short segmentPayloadSize, int newKeepAlivePeriod = 5000)
	{
		var negotiationMessage = sendMessage(new NegotiationMessage(STATE_REQUEST, segmentPayloadSize));
		keepAlive.setTimeout(newKeepAlivePeriod);
		keepAlive.restart();

		if (session == null)
		{
			session = new Session(this, negotiationMessage);
			Console.WriteLine("Session with segment's payload size of " + negotiationMessage.getSegmentPayloadSize() + " was initiated!");
		}
		else
		{
			session.setMetadata(negotiationMessage);
			Console.WriteLine("Session's segment's payload size was updated to " + negotiationMessage.getSegmentPayloadSize() + "!");
		}

		return negotiationMessage;
	}

	public virtual void doTerminate(String msg = "Session and connection terminated!")
	{
		Console.WriteLine(msg);
		sendMessage(newSessionTerminationRequest());

		close();
	}

	public abstract T sendMessage<T>(T message) where T : MessageBase;

	protected abstract void receiveLoop();

	public abstract bool attemptResend(MessageBase message);

	public abstract void resetResendAttempts(int to = 0);

	public abstract void handleKeepAlive(AsyncTimer keepAlive/*, MessageBase keepAliveMessage*/);

	public abstract void handleTransmittedResource(ResourceTransmission finishedResourceTransmission);

	public virtual void start()
	{
		isRunning = true;

		Thread receiveThread = new Thread(receiveLoop);
		receiveThread.Start();
	}

	public virtual void close(int delay = 0)
	{
		if (delay > 0)
			Thread.Sleep(delay);

		isRunning = false;
		udpClient.Close();
		keepAlive.stop();
	}

	public UdpClient getUdpClient()
	{
		return udpClient;
	}

	public IPEndPoint getRemoteSocket()
	{
		return remoteSocket;
	}

	public IPEndPoint getLocalSocket()
	{
		return localSocket;
	}

	public Session getSession()
	{
		return session;
	}

	public void setSession(Session session)
	{
		this.session = session;
	}

	public AsyncTimer getKeepAlive()
	{
		return keepAlive;
	}

	public Socket getTheClient()
	{
		return udpClient.Client;
	}
}