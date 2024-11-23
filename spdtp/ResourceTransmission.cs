using System;
using System.Diagnostics;
using static SpdtpMessage;
using static SpdtpResourceInfoMessage;

/**
* 
*/
public class ResourceTransmission
{
	public static readonly int UNPROCESSED = 0;
	public static readonly int PROCESSED = 1;
	public static readonly int FINISHED = 2;

	protected Connection connection;

	protected SpdtpResourceInfoMessage resourceMetadata;
	
	protected SpdtpResourceSegment[] segments;
	protected int segmentPayloadSize;

	protected int processedSegmentCount, expectedSegmentCount;
	protected int receivedErrorCount = 0;

	protected Stopwatch benchmarkTimer;

	public ResourceTransmission(Connection connection, SpdtpResourceInfoMessage resourceMetadata, SpdtpResourceSegment[] segments = null)
	{
		this.connection = connection;
		setSegments(segments);
		this.resourceMetadata = resourceMetadata;

		segmentPayloadSize = connection.getSession().getMetadata().getSegmentPayloadSize();
	}

	public override String ToString()
	{
		return ToString(true);
	}

	public String ToString(bool verbose = true)
	{
		if (!verbose)
			return GetType().Name + "[" + resourceMetadata.getResourceIdentifier() + ", " + processedSegmentCount + "/" + expectedSegmentCount + "]";

		String str = GetType().Name + "[" + resourceMetadata.getResourceIdentifier() + ", " + processedSegmentCount + "/" + expectedSegmentCount + " |\n";
		for (int i = 0; i < segments.Length; i++) {
			if (segments[i] == null)
				str += "\tnull\n";
			else
			{
				str += "\t" + segments[i] + "\n";
				// str += Utils.formatHeader(segments[i].getBytes());
			}
		}

		return str + "\n]";
	}

	public void start()
	{
		// Console.WriteLine(">" + ToString());
		var senders = new Thread[expectedSegmentCount];
		benchmarkTimer = Stopwatch.StartNew();
		for (int i = 0; i < expectedSegmentCount; i++)
		{
			senders[i] = sendSegmentAsync(i);
		}

		new Thread(() => {
			for (int i = 0; i < expectedSegmentCount; i++)
				senders[i].Join();
			benchmarkTimer.Stop();

			Console.WriteLine("All segments were send in " + benchmarkTimer.ElapsedMilliseconds + "ms!");
		}) { IsBackground = true }.Start();
	}

	public Thread sendSegmentAsync(int segment, String message = "Sending ")
	{
		var sender = new Thread(() => {
			connection.sendMessage(segments[segment]);
			connection.getKeepAlive().restart();
			processedSegmentCount++;

			Console.WriteLine(message + segments[segment] + " asynchronously!");
		}) { IsBackground = true };
		
		sender.Start();
		return sender;
	}

	public void askToResendMissing(int count = 1)
	{
		new Thread(() => {
			for (int i = 0, countToResend = count; i < expectedSegmentCount; i++)
			{
				if (segments[i] == null)
				{
					if (isFinished())
						break;
					connection.sendMessageAsync(new SpdtpResourceSegment(0, i, resourceMetadata.getResourceIdentifier()));
					connection.getKeepAlive().restart();
					if (countToResend-- > 0)
						break;
				}
			}
		}) { IsBackground = true }.Start();;
	}

	public int handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		if (resourceSegment.isState(STATE_REQUEST))
		{
			if (resourceSegment.getPayload() == null)
				return UNPROCESSED;

			int segmentID = resourceSegment.getSegmentID();
			if (!resourceSegment.validate())
			{
				if (segmentID < expectedSegmentCount && segments[segmentID] == null)
				{
					connection.sendMessageAsync(resourceSegment.createResendRequest());
					Console.WriteLine(resourceMetadata.getResourceIdentifier() + ": Segment " + segmentID + " was received with errors, asking for resend!");
				}
				else // This should be very rare...
				{
					Console.WriteLine(resourceMetadata.getResourceIdentifier() + ": Segment was received with erroneous ID, asking to resend the first missing one!");
					askToResendMissing();
				}

				receivedErrorCount++;
				return PROCESSED;
			}

			// TODO timeout
			if (segments[segmentID] == null)
			{
				segments[segmentID] = resourceSegment;
				
				if (processedSegmentCount < 1)
					benchmarkTimer = Stopwatch.StartNew();
				processedSegmentCount++;
				if (receivedErrorCount > 0)
					receivedErrorCount--;

				Console.WriteLine("Segment " + resourceSegment + " received successfully!");
				if (isFinished())
				{
					stop();
					return FINISHED;
				}
			}
			return PROCESSED;
		}

		if (resourceSegment.isState(STATE_RESEND_REQUEST))
		{
			if (!resourceSegment.validate())
			{
				// TODO
			}
			return PROCESSED;
		}

		return UNPROCESSED;
	}

	/**
	* Fragment the resource and populate the SpdtpResourceSegment array...
	* Remember to initialize segments[] correctly in advance...
	*/
	public ResourceTransmission initializeResourceTransmission(byte[] resourceBytes)
	{
		if (segments == null)
			return null;

		int resourceIdentifier = resourceMetadata.getResourceIdentifier();
		for (int i = 0, resourceLen = resourceBytes.Length; i < expectedSegmentCount; i++)
		{
			int start = i * segmentPayloadSize;
			int payloadLength = Math.Min(segmentPayloadSize, resourceLen - start);

			byte[] payload = new byte[payloadLength];
			Array.Copy(resourceBytes, start, payload, 0, payloadLength);

			segments[i] = new SpdtpResourceSegment(STATE_REQUEST, i, resourceIdentifier, payload);
		}

		return this;
	}

	/**
	* Reconstruct the the resource from SpdtpResourceSegment array after it was populated...
	*/
	public byte[] reconstructResource()
	{
		if (segments == null)
			return null;

		byte[] buffer = new byte[segmentPayloadSize * processedSegmentCount];
		int realResourceLength = 0;

		// Console.WriteLine(this);

		for (int i = 0; i < processedSegmentCount; i++)
		{
			byte[] payload = segments[i].getPayload();
			// Console.WriteLine(buffer.Length + ", " + (i * segmentPayloadSize) + ", " + payload.Length);
			Array.Copy(payload, 0, buffer, i * segmentPayloadSize, payload.Length);

			realResourceLength += payload.Length;
		}

		byte[] resourceBytes = new byte[realResourceLength];
		Array.Copy(buffer, 0, resourceBytes, 0, realResourceLength);
		return resourceBytes;
	}

	protected bool isFinished()
	{
		return processedSegmentCount >= expectedSegmentCount;
	}

	public void stop()
	{
		benchmarkTimer.Stop();
		// isRunning = false;
	}

	public void setSegments(SpdtpResourceSegment[] segments)
	{
		this.segments = segments;
		
		if (segments != null)
			setExpectedSegmentCount(segments.Length);
	}

	public SpdtpResourceSegment[] getSegments()
	{
		return segments;
	}

	public int getSegmentPayloadSize() 
	{
		return segmentPayloadSize;
	}
	
	public int getExpectedSegmentCount() 
	{
		return expectedSegmentCount;
	}

	public void setExpectedSegmentCount(int expectedSegmentCount)
	{
		this.expectedSegmentCount = expectedSegmentCount;
	}

	// public void setProcessedSegmentCount(int processed)
	// {
	// 	processedSegmentCount = processed;
	// }

	public SpdtpResourceInfoMessage getMetadata()
	{
		return resourceMetadata;
	}

	public Stopwatch getBenchmarkTimer()
	{
		return benchmarkTimer;
	}
}