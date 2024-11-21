using System;
using static SpdtpMessage;
using static SpdtpResourceInfoMessage;

/**
* 
*/
public class ResourceTransmission
{
	protected Connection connection;

	protected SpdtpResourceInfoMessage resourceMetadata;
	
	protected SpdtpResourceSegment[] segments;
	protected int segmentPayloadSize;

	protected int processedSegmentCount, expectedSegmentCount;
	protected int receivedErrorCount = 0;

	protected bool isRunning;

	public ResourceTransmission(Connection connection, SpdtpResourceInfoMessage resourceMetadata, SpdtpResourceSegment[] segments = null)
	{
		this.connection = connection;
		setSegments(segments);
		this.resourceMetadata = resourceMetadata;
	}

	public override String ToString()
	{
		String str = GetType().Name + "[" + processedSegmentCount + "/" + expectedSegmentCount + " |\n";
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

	public void initiateTransmission()
	{
		// Console.WriteLine(">" + ToString());
		isRunning = true;

		var senders = new Thread[expectedSegmentCount];
		var startTime = DateTime.Now;
		for (int i = 0; i < expectedSegmentCount; i++)
		{
			senders[i] = sendSegmentAsync(i);
		}

		new Thread(() => {
			for (int i = 0; i < expectedSegmentCount; i++)
				senders[i].Join();

			Console.WriteLine("All segments were send in " + (DateTime.Now - startTime).Seconds + "s!");
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
			for (int i = 0, countToResend = count; isRunning && i < expectedSegmentCount; i++)
			{
				if (segments[i] == null)
				{
					connection.sendMessageAsync(new SpdtpResourceSegment(0, i, resourceMetadata.getResourceIdentifier()));
					connection.getKeepAlive().restart();
					if (countToResend-- > 0)
						break;
				}
			}
		}) { IsBackground = true }.Start();;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		//processedSegmentCount++;
		if (resourceSegment.isState(STATE_REQUEST))
		{
			if (resourceSegment.getPayload() == null)
				return false;

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
				return true;
			}

			// TODO timeout
			segments[segmentID] = resourceSegment;
			if (receivedErrorCount > 0)
				receivedErrorCount--;
			return true;
		}

		if (resourceSegment.isState(STATE_RESEND_REQUEST))
		{
			if (!resourceSegment.validate())
			{
				// TODO
			}
			return true;
		}

		return false;
	}

	/**
	* Fragment the resource and populate the SpdtpResourceSegment array...
	* Remember to initialize segments[] correctly in advance...
	*/
	public ResourceTransmission initializeResourceTransmission(byte[] resourceBytes, int segmentPayloadSize)
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

		this.segmentPayloadSize = segmentPayloadSize;
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

		for (int i = 0; i < processedSegmentCount; i++)
		{
			byte[] payload = segments[i].getPayload();
			Array.Copy(payload, 0, buffer, i * segmentPayloadSize, payload.Length);

			realResourceLength += payload.Length;
		}

		byte[] resourceBytes = new byte[realResourceLength];
		Array.Copy(buffer, 0, resourceBytes, 0, realResourceLength);
		return resourceBytes;
	}

	public void finish()
	{
		isRunning = false;
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
	
	public bool isFinished()
	{
		return processedSegmentCount >= expectedSegmentCount;
	}

	// public void setProcessedSegmentCount(int processed)
	// {
	// 	processedSegmentCount = processed;
	// }

	public SpdtpResourceInfoMessage getMetadata()
	{
		return resourceMetadata;
	}

}