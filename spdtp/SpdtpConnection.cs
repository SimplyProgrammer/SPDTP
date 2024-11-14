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
public abstract class SpdtpConnection
{
	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning;

	protected Session session;
	protected AsyncTimer keepAlive;

	public SpdtpConnection(IPEndPoint localSocket, IPEndPoint remoteSocket)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);
	}

	protected virtual void doTerminate(String msg = "Session and connection terminated!")
	{
		Console.WriteLine(msg);
		sendMessage(newSessionTerminationRequest());

		close();
	}

	protected virtual void sendMessageAsync(SpdtpMessage message, bool err = false)
	{
		new Thread(() => sendMessage(message, err))
		{
			IsBackground = true
		}.Start();
	}

	protected abstract T sendMessage<T>(T message, bool err = false) where T : SpdtpMessage;

	protected abstract void receiveLoop();

	public abstract void handleKeepAlive();

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
}