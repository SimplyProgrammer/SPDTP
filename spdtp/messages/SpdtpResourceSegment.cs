using System;
using NullFX.CRC;

public class SpdtpResourceSegment : SpdtpMessage
{
	protected int segmentID; // 24 bits
	protected int resourceIdentifier;

	protected byte[] payload;

	public SpdtpResourceSegment(byte additionalMessageFlags = 0, int segmentID = 0, int resourceIdentifier = 0, byte[] payload = null) : base((byte) (additionalMessageFlags | RESOURCE_SEGMENT), RESOURCE_SEGMENT)
	{
		setSegmentID(segmentID);
		setResourceIdentifier(resourceIdentifier);
		setPayload(payload);
	}

	public override string ToString()
	{
		if (payload != null)
			return GetType().Name + "[" + messageFlags + ", " + segmentID + ", " + resourceIdentifier + ", payload(" + payload.Length + ")]";
		return GetType().Name + "[" + messageFlags + ", " + segmentID + ", " + resourceIdentifier + "]";
	}

	public override SpdtpResourceSegment createResponse(byte additionalFlags = 0)
	{
		return new SpdtpResourceSegment((byte) (STATE_RESPONSE | additionalFlags));
	}

	public override SpdtpResourceSegment createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpResourceSegment((byte) (STATE_RESEND_REQUEST | additionalFlags));
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[123];

		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{
		// TODO
		return this;
	}

	public int getSegmentID()
	{
		return this.segmentID;
	}

	public void setSegmentID(int segmentID)
	{
		this.segmentID = segmentID;
	}

	public int getResourceIdentifier()
	{
		return this.resourceIdentifier;
	}

	public void setResourceIdentifier(int resourceIdentifier)
	{
		this.resourceIdentifier = resourceIdentifier;
	}

	public byte[] getPayload()
	{
		return this.payload;
	}

	public void setPayload(byte[] payload)
	{
		this.payload = payload;
	}
}