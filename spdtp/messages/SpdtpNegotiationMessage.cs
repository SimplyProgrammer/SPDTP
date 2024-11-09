using System;

/**
* NEGOTIATION message.
* Used to open and set session params between peers...
*/
public class SpdtpNegotiationMessage : SpdtpMessage
{
	public static readonly byte SESSION_TERMINATION_8x1 = 0b1111_1111;

	protected short segmentPayloadSize;

	// protected byte checksum;

	public SpdtpNegotiationMessage(byte messageFlags = 0, short segmentPayloadSize = 0) : base(messageFlags, NEGOTIATION)
	{
		this.segmentPayloadSize = segmentPayloadSize;
		// this.checksum = checksum;
	}

	public override string ToString()
	{
		return GetType().Name + "[" + messageFlags + ", " + segmentPayloadSize + "]";
	}

	public override SpdtpMessage createResponse()
	{
		return new SpdtpNegotiationMessage((byte) (NEGOTIATION | getKeepAliveFlag() | STATE_RESPONSE), getSegmentPayloadSize());
	}

	public override SpdtpMessage createResendRequest()
	{
		return new SpdtpNegotiationMessage((byte) (NEGOTIATION | getKeepAliveFlag() | STATE_RESEND_REQUEST), 0);
	}

	public override byte[] getBytes()
	{
		byte[] bytes = new byte[4];
		bytes[0] = getMessageFlags();

		Buffer.BlockCopy(Utils.getBytes(segmentPayloadSize), 0, bytes, 1, sizeof(short));
		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{
		messageFlags = bytes[0];
		
		setSegmentPayloadSize(Utils.getShort(bytes, 1));
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