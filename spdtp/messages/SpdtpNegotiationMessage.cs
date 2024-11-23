using System;
using NullFX.CRC;

/**
* NEGOTIATION message.
* Used to open and set session params between peers...
*/
public class SpdtpNegotiationMessage : SpdtpMessageBase
{
	public static readonly byte SESSION_TERMINATION_8x1 = 0b1111_1111;

	protected short segmentPayloadSize;

	public SpdtpNegotiationMessage(byte additionalMessageFlags = 0, short segmentPayloadSize = 0) : base((byte) (additionalMessageFlags | NEGOTIATION), NEGOTIATION)
	{
		setSegmentPayloadSize(segmentPayloadSize);
		// this.checksum = checksum;
	}

	public override string ToString()
	{
		return GetType().Name + "[" + messageFlags + ", " + segmentPayloadSize + "]";
	}

	public override SpdtpNegotiationMessage createResponse(byte additionalFlags = 0)
	{
		return new SpdtpNegotiationMessage((byte) (STATE_RESPONSE | additionalFlags), getSegmentPayloadSize());
	}

	public override SpdtpNegotiationMessage createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpNegotiationMessage((byte) (STATE_RESEND_REQUEST | additionalFlags));
	}

	public SpdtpNegotiationMessage clone(byte additionalFlags = 0)
	{
		return new SpdtpNegotiationMessage((byte) (getMessageFlags() | additionalFlags), getSegmentPayloadSize());
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[4];
		bytes[0] = getMessageFlags();

		Buffer.BlockCopy(Utils.getBytes((ushort) segmentPayloadSize), 0, bytes, 1, sizeof(short));
		bytes[3] = Crc8.ComputeChecksum(bytes, 0, bytes.Length-1);
		return bytes;
	}

	public override SpdtpMessageBase setFromBytes(byte[] bytes)
	{
		messageFlags = bytes[0];
		
		setSegmentPayloadSize((short) Utils.getShort(bytes, 1));

		isValid = Crc8.ComputeChecksum(bytes, 0, bytes.Length-1) == bytes[3];
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

	public static SpdtpNegotiationMessage newSessionTerminationRequest() // 8x1
	{
		return new SpdtpNegotiationMessage(SESSION_TERMINATION_8x1);
	}
}