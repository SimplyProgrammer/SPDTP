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

	public ResourceTransmission(Connection connection, SpdtpResourceInfoMessage resourceMetadata, SpdtpResourceSegment[] segments = null)
	{
		this.connection = connection;
		setSegments(segments);
		this.resourceMetadata = resourceMetadata;
	}

	public override String ToString()
	{
		String str = GetType().Name + "[" + processedSegmentCount + "/" + expectedSegmentCount + " |\n";
		for (int i = 0; i < segments.Length; i++)
			str += segments[i].ToString();

		return str + "]";
	}

	public void initiateTransmission()
	{
		Console.WriteLine(ToString());
		// for (int i = 0; i < expectedSegmentCount; i++)
		// {
		// 	// TODO
		// }
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		//processedSegmentCount++;
		return false;
	}

	// TODO test

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

	public SpdtpResourceInfoMessage getMetadata()
	{
		return resourceMetadata;
	}
}