using System;
using System.Net;
using System.Net.Sockets;
using static MessageBase;
using static ResourceInfoMessage;

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
		/* Test Negotiation */
		// var NegotiationMessage = new NegotiationMessage((byte) (MessageBase.KEEP_ALIVE | MessageBase.STATE_REQUEST), 123);
		
		// Console.WriteLine(NegotiationMessage);
		// Console.WriteLine(NegotiationMessage.validate());
		// Console.WriteLine(Utils.formatHeader(NegotiationMessage.getBytes()));

		// var newHeader = Utils.introduceRandErrors(NegotiationMessage.getBytes(), 1);
		// var newNegotiationMessage = MessageBase.newMessageFromBytes(newHeader);
		// // Console.WriteLine(NegotiationMessage.getKeepAliveFlag());
		
		// Console.WriteLine(NegotiationMessage);
		// Console.WriteLine(newNegotiationMessage.validate());
		// Console.WriteLine(Utils.formatHeader(newHeader));

		// Console.WriteLine();

		/* Test resource info */
		// Console.WriteLine(Utils.getHashIdentifier("hii"));

		// var MessageBase = new ResourceInfoMessage(MessageBase.STATE_REQUEST, 123, ResourceInfoMessage.TEXT_MSG_MARK + "Hello!");
		
		// Console.WriteLine(MessageBase);
		// Console.WriteLine(MessageBase.validate());
		// Console.WriteLine(MessageBase.getResourceIdentifier());
		// Console.WriteLine(Utils.formatHeader(MessageBase.getBytes()) + "\n");

		// var newHeader = Utils.introduceRandErrors(MessageBase.getBytes(), 1);

		// var newMessageBase = MessageBase.newMessageFromBytes(newHeader);

		// Console.WriteLine(newMessageBase);
		// Console.WriteLine(newMessageBase.validate());
		// Console.WriteLine(MessageBase.getResourceIdentifier());
		// // Console.WriteLine(NegotiationMessage.getKeepAliveFlag());
		// Console.WriteLine(Utils.formatHeader(newHeader));
		
		// Console.WriteLine(newMessageBase);

		/* Test trans */
		// FileStream resource = File.Open("./data/test_7221_2mb.jpg", FileMode.Open, FileAccess.Read);
		
		// byte[] bytes = new byte[resource.Length];
		// resource.Read(bytes, 0, bytes.Length);
		// resource.Close(); 

		// Console.WriteLine(bytes.Length);

		// int segmentPayloadSize = 123;

		// var segmentCount = (bytes.Length - 1) / segmentPayloadSize + 1;
		// var pendingResourceInfoMessage = new ResourceInfoMessage(STATE_REQUEST, segmentCount, Utils.truncString(((FileStream) resource).Name, 64));

		// var resourceTrans = new ResourceTransmission(null, pendingResourceInfoMessage, new ResourceSegment[segmentCount]);
		// resourceTrans.initializeResourceTransmission(bytes, segmentPayloadSize);

		// Console.WriteLine(resourceTrans);

		// var segment = resourceTrans.getSegments()[resourceTrans.getExpectedSegmentCount()-1].createResendRequest();
		// Console.WriteLine(segment);
		// Console.WriteLine(segment.validate());
		// Console.WriteLine(segment.getResourceIdentifier());
		// Console.WriteLine(Utils.formatHeader(segment.getBytes()) + "\n");

		// resourceTrans.setProcessedSegmentCount(resourceTrans.getExpectedSegmentCount());
		// byte[] reconstructedBytes = resourceTrans.reconstructResource();
		// Console.WriteLine(reconstructedBytes.Length);

		// var fs = new FileStream("./data/_results/_test_7221_2mb.jpg", FileMode.Create, FileAccess.Write);
		// fs.Write(reconstructedBytes, 0, reconstructedBytes.Length);
		// fs.Close();

		// var MessageBase = new ResourceSegment(STATE_REQUEST, 0, 1456, new byte[] {100, 101, 102});
		
		// Console.WriteLine(MessageBase);
		// Console.WriteLine(MessageBase.validate());
		// Console.WriteLine(MessageBase.getResourceIdentifier());
		// Console.WriteLine(Utils.formatHeader(MessageBase.getBytes()) + "\n");

		// var newHeader = Utils.introduceRandErrors(MessageBase.getBytes(), 0);

		// ResourceSegment newMessageBase = (ResourceSegment)MessageBase.newMessageFromBytes(newHeader);

		// Console.WriteLine(newMessageBase);
		// Console.WriteLine(newMessageBase.validate());
		// Console.WriteLine(newMessageBase.getResourceIdentifier());
		// // Console.WriteLine(NegotiationMessage.getKeepAliveFlag());
		// Console.WriteLine(Utils.formatHeader(newHeader)+ "\n\n"); 

		// var responseMsg = MessageBase.createResendRequest();
		
		// Console.WriteLine(responseMsg);
		// Console.WriteLine(responseMsg.validate());
		// Console.WriteLine(responseMsg.getResourceIdentifier());
		// Console.WriteLine(Utils.formatHeader(responseMsg.getBytes()) + "\n");

		// var newResponseHeader = Utils.introduceRandErrors(responseMsg.getBytes(), 0);

		// ResourceSegment newResponseMsg = (ResourceSegment) newMessageFromBytes(newResponseHeader);

		// Console.WriteLine(newResponseMsg);
		// Console.WriteLine(newResponseMsg.validate());
		// Console.WriteLine(newResponseMsg.getResourceIdentifier());
		// // Console.WriteLine(NegotiationMessage.getKeepAliveFlag());
		// Console.WriteLine(Utils.formatHeader(newResponseHeader)); 
	}

	public static String cypher(String msg)
	{
		String result = "";
		for (int i = 0; i < msg.Length; i++)
		{
			if (i+1 >= msg.Length)
				break;
			result += msg[i+1];
			result += msg[i];
			result += " ";

			i+=1;
		}

		return result;
	}

	public static void Main(string[] args)
	{
		// test();

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

		// Console.Clear();
		List<String> ips = new List<String>();
		IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
		foreach (IPAddress ip in entry.AddressList)
			if (ip.AddressFamily == AddressFamily.InterNetwork)
				ips.Add(ip.ToString());

		while (true)
		{
			try
			{
				int defaultSpdtpPort = 18800;
				IPEndPoint localPoint, remotePoint;

				Console.Write("Enter your local socket address: ");
				String localSocketAddress = Console.ReadLine();
				if (localSocketAddress?.ToLower() == "exit")
					break;
		
				String[] localIpAndPort = localSocketAddress.Split(':');
				if (localIpAndPort[0].Length < 1)
					localIpAndPort[0] = "127.0.0.1";
	
				int localPort = localIpAndPort[1].StartsWith("+") ? defaultSpdtpPort+1 : (localIpAndPort[1].StartsWith("-") ? defaultSpdtpPort-1 : (localIpAndPort[1].Length < 1 ? defaultSpdtpPort : short.Parse(localIpAndPort[1])));
				localPoint = new IPEndPoint(IPAddress.Parse(localIpAndPort[0] = localIpAndPort[0].Replace("localhost", "127.0.0.1").Replace("ip", ips[0])), localPort);

				Console.Write("Enter remote socket address: ");
				String remoteSocketAddress = Console.ReadLine();
				if (remoteSocketAddress?.ToLower() == "exit")
					break;

				String[] remoteIpAndPort = remoteSocketAddress.Split(':');
				if (remoteIpAndPort[0].Length < 1)
					remoteIpAndPort[0] = localIpAndPort[0];

				int remotePort = remoteIpAndPort[1].StartsWith("+") ? localPort+1 : (remoteIpAndPort[1].StartsWith("-") ? localPort-1 : remoteIpAndPort[1].Length < 1 ? localPort : short.Parse(remoteIpAndPort[1]));
				remotePoint = new IPEndPoint(IPAddress.Parse(remoteIpAndPort[0].Replace("localhost", "127.0.0.1").Replace("ip", ips[0])), remotePort);

				var peer = new CliPeer(localPoint, remotePoint);
				peer.verbose = args.Length > 1 && args[1].StartsWith("-") && args[1].Contains("v");
				peer.start();
			}
			catch (Exception ex) 
			{
				Console.Error.WriteLine("Error has occurred: " + ex.Message);
			}
		}
	}
}