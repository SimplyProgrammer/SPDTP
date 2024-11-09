using System;
using System.Net;
using System.Net.Sockets;

public class Program
{
	public static void Main(string[] args)
	{
		// SpdtpMessage spdtpNegotiationMessage = new SpdtpNegotiationMessage((byte) (SpdtpMessage.NEGOTIATION | SpdtpMessage.KEEP_ALIVE | SpdtpMessage.STATE_REQUEST), 123);
		// Console.WriteLine(spdtpNegotiationMessage.validate());
		
		// Console.WriteLine(spdtpNegotiationMessage);

		// Utils.printHeader(spdtpNegotiationMessage.getBytes());

		// spdtpNegotiationMessage = SpdtpMessage.newMessageFromBytes(spdtpNegotiationMessage.getBytes());
		// Console.WriteLine(spdtpNegotiationMessage.validate());
		// Console.WriteLine(spdtpNegotiationMessage.isKeepAlive());
		
		// Console.WriteLine(spdtpNegotiationMessage);

		// AsyncTimer timer = new AsyncTimer(() => Console.WriteLine("haha"), 3000).start();

		// timer.restart();

		// Console.ReadLine();

		while (true)
		{
			try 
			{
				Console.Write("Enter your local socket address: ");
				String localSocketAddress = Console.ReadLine();
				if (localSocketAddress?.ToLower() == "exit")
					break;

				String[] localIpAndPort = localSocketAddress.Split(":");
				if (localIpAndPort[0].Length < 1)
					localIpAndPort[0] = "127.0.0.1";
				IPEndPoint localPoint = new IPEndPoint(IPAddress.Parse(localIpAndPort[0].Replace("localhost", "127.0.0.1")), short.Parse(localIpAndPort[1]));

				Console.Write("Enter remote socket address: ");
				String remoteSocketAddress = Console.ReadLine();
				if (remoteSocketAddress?.ToLower() == "exit")
					break;

				String[] remoteIpAndPort = remoteSocketAddress.Split(":");
				if (remoteIpAndPort[0].Length < 1)
					remoteIpAndPort[0] = "127.0.0.1";
				IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAndPort[0].Replace("localhost", "127.0.0.1")), short.Parse(remoteIpAndPort[1]));

				SpdtpPeer peer = new SpdtpPeer(localPoint, remotePoint);
				peer.start();
			}
			catch (Exception ex) {
				Console.Error.WriteLine("Error has occurred: " + ex);
			}
		}
	}
}