using System;
using NullFX.CRC;
using System.Text;

/**
* INCOMING_RESOURCE_INFO message.
* Used to inform the other peer about the incoming resource so he can act accordingly...
*/
public class SpdtpResourceInfoMessage : SpdtpMessage
{
	protected int segmentCount; // 24 bits
	protected String resourceName;

	public SpdtpResourceInfoMessage(byte additionalMessageFlags = 0, int segmentCount = 0, String resourceName = "") : base((byte) (additionalMessageFlags | INCOMING_RESOURCE_INFO), INCOMING_RESOURCE_INFO)
	{
		setSegmentCount(segmentCount);
		setResourceName(resourceName);
		// this.checksum = checksum;
	}

	public override string ToString()
	{
		return GetType().Name + "[" + messageFlags + ", " + segmentCount + ", " + resourceName + "]";
	}

	public override SpdtpResourceInfoMessage createResponse(byte additionalFlags = 0)
	{
		return new SpdtpResourceInfoMessage((byte) (getKeepAliveFlag() | STATE_RESPONSE | additionalFlags), getSegmentCount(), getResourceName());
	}

	public override SpdtpResourceInfoMessage createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpResourceInfoMessage((byte) (getKeepAliveFlag() | STATE_RESEND_REQUEST | additionalFlags));
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[4+2+resourceName.Length];
		bytes[0] = getMessageFlags();

		Buffer.BlockCopy(Utils.getBytes(segmentCount), 1, bytes, 1, 3);

		byte[] resourceNameBytes = Encoding.ASCII.GetBytes(resourceName);
		Buffer.BlockCopy(resourceNameBytes, 0, bytes, 4, resourceNameBytes.Length);

		var crc16 = Crc16.ComputeChecksum(Crc16Algorithm.Standard, bytes, 0, bytes.Length-2);
		Buffer.BlockCopy(Utils.getBytes(crc16), 0, bytes, bytes.Length-2, 2);
		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{
		messageFlags = bytes[0];

		setSegmentCount(Utils.getInt24(bytes, 1));

		resourceName = Encoding.ASCII.GetString(bytes, 4, bytes.Length-6);

		isValid = Crc16.ComputeChecksum(Crc16Algorithm.Standard, bytes, 0, bytes.Length-2) == Utils.getShort(bytes, bytes.Length-2);
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