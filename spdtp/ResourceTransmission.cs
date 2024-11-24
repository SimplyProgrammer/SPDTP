using System;
using System.Diagnostics;
using static SpdtpMessageBase;
using static SpdtpResourceInfoMessage;

/**
* Session responsible for transmitting single resource, it exists both on sender and receiver side!
*/
public class ResourceTransmission : SessionBase<SpdtpResourceInfoMessage, SpdtpResourceSegment, int>
{
	public static readonly int UNPROCESSED = 0;
	public static readonly int PROCESSED = 1;
	public static readonly int FINISHED = 2;
	
	protected SpdtpResourceSegment[] segments;
	protected int segmentPayloadSize;

	protected int processedSegmentCount, expectedSegmentCount;
	protected int receivedErrorCount = 0;

	// protected int lastErrorIndex = 0;

	protected Stopwatch benchmarkTimer;

	public ResourceTransmission(Connection connection, SpdtpResourceInfoMessage resourceMetadata, SpdtpResourceSegment[] segments = null) : base(connection, resourceMetadata)
	{
		setSegments(segments);

		segmentPayloadSize = connection.getSession().getMetadata().getSegmentPayloadSize();
	}

	public override String ToString()
	{
		return ToString(true);
	}

	public String ToString(bool verbose = true)
	{
		if (!verbose)
			return GetType().Name + "[" + metadata.getResourceIdentifier() + ", " + processedSegmentCount + "/" + expectedSegmentCount + "]";

		String str = GetType().Name + "[" + metadata.getResourceIdentifier() + ", " + processedSegmentCount + "/" + expectedSegmentCount + " |\n";
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
		benchmarkTimer = Stopwatch.StartNew();

		if (segments.Length != expectedSegmentCount)
			Console.WriteLine("Warning segment.Length does not match expectedSegmentCount!");

		new Thread(() => 
		{
			for (int i = 0; i < expectedSegmentCount; i++)
			{
				connection.sendMessage(segments[i]);
				connection.getKeepAlive().restart();
				processedSegmentCount++;

				Console.WriteLine("Sending " + segments[i] + "!");
			}

			benchmarkTimer.Stop();
			Console.WriteLine("All segments of " + ToString(false) + " were send in " + benchmarkTimer.ElapsedMilliseconds + "ms!");
		}) { IsBackground = true }.Start();
	}

	public void askToResendMissing(int count = 1)
	{
		new Thread(() => {
			for (int i = 0; i < expectedSegmentCount; i++)
			{
				if (segments[i] == null)
				{
					if (count-- < 1 || isFinished())
						break;

					connection.sendMessage(new SpdtpResourceSegment(0, i, metadata.getResourceIdentifier()));
					connection.getKeepAlive().restart();
					// lastErrorIndex = i
				}
			}
		}) { IsBackground = true }.Start();
	}

	public override int handleIncomingMessage(SpdtpResourceSegment resourceSegment)
	{
		if (resourceSegment.isState(STATE_REQUEST))
		{
			if (resourceSegment.getPayload() == null)
				return UNPROCESSED;

			int segmentID = resourceSegment.getSegmentID();
			if (!resourceSegment.validate())
			{
				if (segmentID < expectedSegmentCount && segments[segmentID] == null) // Segment id seems to be legit, we can trust it...
				{
					connection.sendMessage(resourceSegment.createResendRequest());
					Console.WriteLine(metadata.getResourceIdentifier() + ": Segment " + segmentID + " was received with errors, asking for resend!");
				}
				else // This should be very rare...
				{
					Console.WriteLine(metadata.getResourceIdentifier() + ": Segment was received with erroneous ID, asking to resend the first missing one!");
					askToResendMissing();
				}

				if (receivedErrorCount++ > Connection.ACCEPTABLE_ERR_COUNT)
					connection.doTerminate("Session and connection terminated (too many transmission errors)!");
				return PROCESSED;
			}

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

				return PROCESSED;
			}

			askToResendMissing();
			Console.WriteLine("Segment " + resourceSegment + " was already received, asking to resend first missing one!");
			return PROCESSED;
		}

		if (resourceSegment.isState(STATE_RESEND_REQUEST))
		{
			int segmentID = resourceSegment.getSegmentID();
			if (!resourceSegment.validate())
			{
				if (!(segmentID < expectedSegmentCount)) // Segment ID seems to be faulty, lets try to "correct" it and resend "random" segment. If we miss, receiver should ask again...
					segmentID %= expectedSegmentCount;
			}

			connection.sendMessage(segments[segmentID]);
			Console.WriteLine(metadata.getResourceIdentifier() + ": Resending segment " + segmentID + "!");
			return PROCESSED;
		}

		return UNPROCESSED;
	}

	public override void onKeepAlive()
	{
		if (!isFinished())
			askToResendMissing(getExpectedSegmentCount() - processedSegmentCount);
	}

	/**
	* Fragment the resource and populate the SpdtpResourceSegment array...
	* Remember to initialize segments[] correctly in advance...
	*/
	public ResourceTransmission initializeResourceTransmission(byte[] resourceBytes)
	{
		if (segments == null || segments.Length < 1)
			return null;

		int resourceIdentifier = metadata.getResourceIdentifier();
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
		if (segments == null || segments.Length < 1)
			return null;

		// Console.WriteLine(this);

		byte[] resourceBytes = new byte[segmentPayloadSize * (processedSegmentCount-1) + segments[processedSegmentCount-1].getPayload().Length];
		for (int i = 0; i < processedSegmentCount; i++)
		{
			byte[] payload = segments[i].getPayload();
			// Console.WriteLine(buffer.Length + ", " + (i * segmentPayloadSize) + ", " + payload.Length);
			Array.Copy(payload, 0, resourceBytes, i * segmentPayloadSize, payload.Length);
		}

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

	public Stopwatch getBenchmarkTimer()
	{
		return benchmarkTimer;
	}
}