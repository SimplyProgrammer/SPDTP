using System;
using NullFX.CRC;

/**
* NEGOTIATION message.
* Used to open and set session params between peers...
*/
public class SpdtpNegotiationMessage : SpdtpMessage
{
	public static readonly byte SESSION_TERMINATION_8x1 = 0b1111_1111;

	protected short segmentPayloadSize;

	public SpdtpNegotiationMessage(byte messageFlags = 0, short segmentPayloadSize = 0) : base(messageFlags, NEGOTIATION)
	{
		this.segmentPayloadSize = segmentPayloadSize;
		// this.checksum = checksum;
	}

	public override string ToString()
	{
		return GetType().Name + "[" + messageFlags + ", " + segmentPayloadSize + "]";
	}

	public override SpdtpNegotiationMessage createResponse(byte additionalFlags = 0)
	{
		return new SpdtpNegotiationMessage((byte) (NEGOTIATION | getKeepAliveFlag() | STATE_RESPONSE | additionalFlags), getSegmentPayloadSize());
	}

	public override SpdtpNegotiationMessage createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpNegotiationMessage((byte) (NEGOTIATION | getKeepAliveFlag() | STATE_RESEND_REQUEST | additionalFlags), 0);
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[4];
		bytes[0] = getMessageFlags();

		Buffer.BlockCopy(Utils.getBytes(segmentPayloadSize), 0, bytes, 1, sizeof(short));
		bytes[3] = Crc8.ComputeChecksum(bytes, 0, 3);
		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{
		messageFlags = bytes[0];
		
		setSegmentPayloadSize(Utils.getShort(bytes, 1));
		isValid = Crc8.ComputeChecksum(bytes, 0, 3) == bytes[3];
		return this;
	}

	public short getSegmentPayloadSize()
	{
		return segmentPayloadSize;
	}

	public void setSegmentPayloadSize(short newSegmentPayloadSize)
	{
		segmentPayloadSize = newSegmentPayloadSize;
	}

	// public byte getChecksum()
	// {
	// 	return checksum;
	// }

	// public void setChecksum(byte newChecksum)
	// {
	// 	checksum = newChecksum;
	// }

	public static SpdtpNegotiationMessage newSessionTerminationRequest()
	{
		return new SpdtpNegotiationMessage(SESSION_TERMINATION_8x1);
	}
}