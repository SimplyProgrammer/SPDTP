using System;
using System.Net;
using System.Net.Sockets;

public class Program
{
	// static AsyncTimer a(int additionalCount = 0, int period = 5000)
	// {
	// 	new Thread(() => Console.WriteLine("test"))
	// 	{
	// 		IsBackground = true
	// 	}.Start();

	// 	if (additionalCount > 0)
	// 		return new AsyncTimer((self) => {
	// 			if (self.getTimeoutCount() > additionalCount)
	// 				self.stop(false, true);
	// 			else
	// 				Console.WriteLine("test");
	// 		}, period).start();

	// 	return null;
	// }

	public static void test()
	{
		// var spdtpNegotiationMessage = new SpdtpNegotiationMessage((byte) (SpdtpMessage.KEEP_ALIVE | SpdtpMessage.STATE_REQUEST), 123);
		
		// Console.WriteLine(spdtpNegotiationMessage);
		// Console.WriteLine(spdtpNegotiationMessage.validate());
		// Console.WriteLine(Utils.formatHeader(spdtpNegotiationMessage.getBytes()));

		// var newHeader = Utils.introduceRandErrors(spdtpNegotiationMessage.getBytes(), 1);
		// var newSpdtpNegotiationMessage = SpdtpMessage.newMessageFromBytes(newHeader);
		// // Console.WriteLine(spdtpNegotiationMessage.getKeepAliveFlag());
		
		// Console.WriteLine(spdtpNegotiationMessage);
		// Console.WriteLine(newSpdtpNegotiationMessage.validate());
		// Console.WriteLine(Utils.formatHeader(newHeader));

		// Console.WriteLine();

		Console.WriteLine("hii".GetHashCode());

		var spdtpMessage = new SpdtpResourceInfoMessage(SpdtpMessage.STATE_REQUEST, 123, SpdtpResourceInfoMessage.TEXT_MSG_MARK + "Helloo!");
		
		Console.WriteLine(spdtpMessage);
		Console.WriteLine(spdtpMessage.validate());
		Console.WriteLine(spdtpMessage.getResourceIdentifier());
		Console.WriteLine(Utils.formatHeader(spdtpMessage.getBytes()) + "\n");

		var newHeader = Utils.introduceRandErrors(spdtpMessage.getBytes(), 1);

		var newSpdtpMessage = SpdtpMessage.newMessageFromBytes(newHeader);

		Console.WriteLine(newSpdtpMessage);
		Console.WriteLine(newSpdtpMessage.validate());
		Console.WriteLine(spdtpMessage.getResourceIdentifier());
		// Console.WriteLine(spdtpNegotiationMessage.getKeepAliveFlag());
		Console.WriteLine(Utils.formatHeader(newHeader));
		
		Console.WriteLine(newSpdtpMessage);
	}

	public static void Main(string[] args)
	{
		test();

		// AsyncTimer timer = new AsyncTimer(() => Console.WriteLine("haha"), 3000).start();

		// timer.restart();

		// Console.WriteLine(String.Join(", ", args));

		// var timer = a(2);
		// timer.setOnStopCallback(self => { 
		// 	Console.WriteLine(self.getTimeoutCount());
		// 	Console.WriteLine("timeout");
		// });

		// Console.ReadLine();
		// timer.stop(false);

		// uint unint = 4294967205;
		// int sint = -91;

		// Console.WriteLine(Utils.formatHeader(Utils.getBytes((int) unint)) + "\n" + Utils.formatHeader(Utils.getBytes(sint)) + "\n" + (uint) Utils.getInt(Utils.getBytes((int) unint)) + "\n" + (unint == sint) + "\n" + (unint == (uint) sint));

		// while (true)
		// {
		// 	try 
		// 	{
		// 		IPEndPoint localPoint, remotePoint;

		// 		Console.Write("Enter your local socket address: ");
		// 		String localSocketAddress = Console.ReadLine();
		// 		if (localSocketAddress?.ToLower() == "exit")
		// 			break;
		
		// 		String[] localIpAndPort = localSocketAddress.Split(':');
		// 		if (localIpAndPort[0].Length < 1)
		// 			localIpAndPort[0] = "127.0.0.1";
		// 		localPoint = new IPEndPoint(IPAddress.Parse(localIpAndPort[0].Replace("localhost", "127.0.0.1")), short.Parse(localIpAndPort[1]));

		// 		Console.Write("Enter remote socket address: ");
		// 		String remoteSocketAddress = Console.ReadLine();
		// 		if (remoteSocketAddress?.ToLower() == "exit")
		// 			break;

		// 		String[] remoteIpAndPort = remoteSocketAddress.Split(':');
		// 		if (remoteIpAndPort[0].Length < 1)
		// 			remoteIpAndPort[0] = localIpAndPort[0];

		// 		int remotePort = remoteIpAndPort[1].StartsWith("+") ? short.Parse(localIpAndPort[1])+1 : (remoteIpAndPort[1].StartsWith("-") ? short.Parse(localIpAndPort[1])-1 : short.Parse(remoteIpAndPort[1]));
		// 		remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAndPort[0].Replace("localhost", "127.0.0.1")), remotePort);

		// 		var peer = new CliPeer(localPoint, remotePoint);
		// 		peer.verbose = args.Length > 1 && args[1].StartsWith("-") && args[1].Contains("v");
		// 		peer.start();
		// 	}
		// 	catch (Exception ex) {
		// 		Console.Error.WriteLine("Error has occurred: " + ex);
		// 	}
		// }
	}
}