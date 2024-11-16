using System;
using static SpdtpMessage;
using static SpdtpResourceInfoMessage;

/**
* 
*/
public class ResourceTransmission
{
	protected SpdtpConnection connection;

	protected SpdtpResourceInfoMessage resourceMetadata;
	
	protected SpdtpResourceSegment[] segments;
	protected int segmentPayloadSize; 

	public ResourceTransmission(SpdtpConnection connection, SpdtpResourceInfoMessage resourceMetadata, SpdtpResourceSegment[] segments = null)
	{
		this.connection = connection;
		this.segments = segments;
		this.resourceMetadata = resourceMetadata;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		
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
		for (int i = 0, resourceLen = resourceBytes.Length; i < segments.Length; i++)
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

		byte[] buffer = new byte[segmentPayloadSize * segments.Length];
		int realResourceLength = 0;

		for (int i = 0; i < segments.Length; i++)
		{
			byte[] payload = segments[i].getPayload();
			Array.Copy(payload, 0, buffer, i * segmentPayloadSize, payload.Length);

			realResourceLength += payload.Length;
		}

		byte[] resourceBytes = new byte[realResourceLength];
		Array.Copy(buffer, 0, resourceBytes, 0, realResourceLength);
		return resourceBytes;
	}
}