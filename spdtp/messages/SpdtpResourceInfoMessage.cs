using System;
using NullFX.CRC;

/**
* INCOMING_RESOURCE_INFO message.
* Used to inform the other peer about the incoming resource so he can act accordingly...
*/
public class SpdtpResourceInfoMessage : SpdtpMessage
{
	protected int segmentCount;
	protected String resourceName;

	public SpdtpResourceInfoMessage(byte messageFlags = 0, int segmentCount = 0, String resourceName = null) : base(messageFlags, INCOMING_RESOURCE_INFO)
	{
		this.segmentCount = segmentCount;
		// this.checksum = checksum;
	}

	public override string ToString()
	{
		return GetType().Name + "[" + messageFlags + ", " + segmentCount + "]";
	}

	public override SpdtpResourceInfoMessage createResponse(byte additionalFlags = 0)
	{
		return new SpdtpResourceInfoMessage((byte) (INCOMING_RESOURCE_INFO | getKeepAliveFlag() | STATE_RESPONSE | additionalFlags), getSegmentCount(), getResourceName());
	}

	public override SpdtpResourceInfoMessage createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpResourceInfoMessage((byte) (INCOMING_RESOURCE_INFO | getKeepAliveFlag() | STATE_RESEND_REQUEST | additionalFlags), 0);
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[66];

		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{

		return this;
	}

	public int getSegmentCount()
	{
		return segmentCount;
	}

	public void setSegmentCount(int segmentCount)
	{
		this.segmentCount = segmentCount;
	}

	public String getResourceName()
	{
		return resourceName;
	}

	public void setResourceName(String resourceName)
	{
		this.resourceName = resourceName;
	}
}