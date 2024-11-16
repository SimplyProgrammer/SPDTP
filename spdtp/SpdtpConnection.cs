using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using static SpdtpMessage;
using static SpdtpNegotiationMessage;

/**
* The abstraction of SpdtpConnection (peer)
*/
public abstract class SpdtpConnection
{
	protected UdpClient udpClient;
	protected IPEndPoint remoteSocket;
	protected IPEndPoint localSocket;

	protected bool isRunning;

	protected Session session;
	protected AsyncTimer keepAlive;

	public SpdtpConnection(IPEndPoint localSocket, IPEndPoint remoteSocket, int keepAlivePeriod)
	{
		this.localSocket = localSocket;
		this.remoteSocket = remoteSocket;

		udpClient = new UdpClient(localSocket);

		keepAlive = new AsyncTimer(handleKeepAlive, keepAlivePeriod);
	}

	public virtual SpdtpNegotiationMessage openSession(short segmentPayloadSize, int newKeepAlivePeriod = 5000, bool err = false)
	{
		var negotiationMessage = sendMessage(new SpdtpNegotiationMessage(STATE_REQUEST, segmentPayloadSize), err);
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

	public virtual AsyncTimer sendMessageAsync(SpdtpMessage message, int reattemptCount = 0, int period = 5000, bool err = false)
	{
		new Thread(() => sendMessage(message, err))
		{
			IsBackground = true
		}.Start();

		if (reattemptCount > 0)
			return new AsyncTimer(self => 
			{
				if (self.getTimeoutCount() > reattemptCount)
					self.stop(false, true);
				else
				{
					sendMessage(message, err);
					keepAlive.restart();
				}

			}, period).start();

		return null;
	}

	public abstract T sendMessage<T>(T message, bool err = false) where T : SpdtpMessage;

	protected abstract void receiveLoop();

	public abstract bool attemptResend(SpdtpMessage message);

	public abstract void resetResendAttempts(int to = 0);

	public abstract void handleKeepAlive(AsyncTimer keepAlive/*, SpdtpMessage keepAliveMessage*/);

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