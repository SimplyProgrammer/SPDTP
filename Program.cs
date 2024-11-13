using System;
using System.Net;
using System.Net.Sockets;

public class Program
{
	public static void Main(string[] args)
	{
		// var spdtpNegotiationMessage = new SpdtpNegotiationMessage((byte) (SpdtpMessage.NEGOTIATION | SpdtpMessage.KEEP_ALIVE | SpdtpMessage.STATE_REQUEST), 123);
		// Console.WriteLine(spdtpNegotiationMessage.validate());
		
		// Console.WriteLine(spdtpNegotiationMessage);

		// Console.WriteLine(Utils.formatHeader(spdtpNegotiationMessage.getBytes()));

		// var newHeader = Utils.introduceRandErrors(spdtpNegotiationMessage.getBytes(), 1);
		// var newSpdtpNegotiationMessage = SpdtpMessage.newMessageFromBytes(newHeader);
		// Console.WriteLine(newSpdtpNegotiationMessage.validate());
		// // Console.WriteLine(spdtpNegotiationMessage.getKeepAliveFlag());
		// Console.WriteLine(newSpdtpNegotiationMessage);
		// Console.WriteLine(Utils.formatHeader(newHeader));
		
		// Console.WriteLine(spdtpNegotiationMessage);

		// AsyncTimer timer = new AsyncTimer(() => Console.WriteLine("haha"), 3000).start();

		// timer.restart();

		// Console.WriteLine(String.Join(", ", args));
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

				var peer = new SpdtpCliPeer(localPoint, remotePoint);
				peer.verbose = args.Length > 0 && args[0].StartsWith("-") && args[0].Contains("v");
				peer.start();
			}
			catch (Exception ex) {
				Console.Error.WriteLine("Error has occurred: " + ex);
			}
		}
	}
}