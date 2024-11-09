using System;
using System.Net;
using System.Net.Sockets;

public class Program
{
	public static void Main(string[] args)
	{
		SpdtpNegotiationMessage spdtpNegotiationMessage = new SpdtpNegotiationMessage((byte) (SpdtpMessage.NEGOTIATION | SpdtpMessage.STATE_REQUEST), 123);
		Console.WriteLine(spdtpNegotiationMessage);

		// while (true)
		// {
		// 	try 
		// 	{
		// 		Console.Write("Enter your local socket address: ");
		// 		String localSocketAddress = Console.ReadLine();
		// 		if (localSocketAddress?.ToLower() == "exit")
		// 			break;

		// 		String[] localIpAndPort = localSocketAddress.Split(":");
		// 		IPEndPoint localPoint = new IPEndPoint(IPAddress.Parse(localIpAndPort[0].Replace("localhost", "127.0.0.1")), short.Parse(localIpAndPort[1]));

		// 		Console.Write("Enter remote socket address: ");
		// 		String remoteSocketAddress = Console.ReadLine();
		// 		if (remoteSocketAddress?.ToLower() == "exit")
		// 			break;

		// 		String[] remoteIpAndPort = remoteSocketAddress.Split(":");
		// 		IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAndPort[0].Replace("localhost", "127.0.0.1")), short.Parse(remoteIpAndPort[1]));

		// 		SpdtpPeer peer = new SpdtpPeer(localPoint, remotePoint);
		// 		peer.start();
		// 	}
		// 	catch (Exception ex) {
		// 		Console.Error.WriteLine("Error has occurred: " + ex);
		// 	}
		// }
	}
}