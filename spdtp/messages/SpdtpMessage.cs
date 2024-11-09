using System;

/**
* This is common message type of Spdtp.
*/
public abstract class SpdtpMessage
{
	public static readonly byte NEGOTIATION = 0b0100_0000;
	public static readonly byte INCOMING_RESOURCE_INFO = 0b1000_0000;
	public static readonly byte RESOURCE_SEGMENT = 0b1100_0000;

	public static readonly byte KEEP_ALIVE = 0b0010_0000;

	public static readonly byte STATE_RESEND_REQUEST = 0;
	public static readonly byte STATE_REQUEST = 1;
	public static readonly byte STATE_RESPONSE = 2;

	protected byte messageFlags, type;

	public SpdtpMessage(byte messageFlags = 0, byte type = 0)
	{
		this.messageFlags = messageFlags;

		this.type = type;
	}

	public abstract SpdtpMessage createResponse();
	
	public abstract SpdtpMessage createResendRequest();

	public abstract byte[] getBytes();

	public abstract SpdtpMessage setFromBytes(byte[] bytes);

	public virtual bool validate()
	{
		return (byte) (messageFlags & 0b0001_1100) == 0 && type == (byte) (messageFlags & 0b1100_0000);
	}

	public byte getMessageFlags()
	{
		return messageFlags;
	}

	public byte getType()
	{
		return type;
	}

	public virtual byte getTransmissionState()
	{
		return (byte) (messageFlags & 0b11);
	}

	public virtual bool isState(byte state)
	{
		return getTransmissionState() == state;
	}

	public int getKeepAliveFlag()
	{
		return messageFlags & 0b0010_0000;
	}

	public static SpdtpMessage newMessageFromBytes(byte[] bytes)
	{
		if (bytes.Length == 4)
		{
			return new SpdtpNegotiationMessage().setFromBytes(bytes);
		}

		if (bytes.Length == 70)
		{
			
		}

		return null;
	}
}