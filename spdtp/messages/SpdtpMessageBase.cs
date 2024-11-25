using System;

/**
* This is common message type of Spdtp.
*/
public abstract class SpdtpMessageBase
{
	public static readonly byte NEGOTIATION = 0b0100_0000;
	public static readonly byte INCOMING_RESOURCE_INFO = 0b1000_0000;
	public static readonly byte RESOURCE_SEGMENT = 0b1100_0000;

	public static readonly byte KEEP_ALIVE = 0b0010_0000;

	public static readonly byte STATE_RESEND_REQUEST = 0; //NACK
	public static readonly byte STATE_REQUEST = 1; //SYN
	public static readonly byte STATE_RESPONSE = 2; //ACK

	protected byte messageFlags, type;

	protected bool isValid = true;

	public SpdtpMessageBase(byte messageFlags = 0, byte type = 0)
	{
		this.messageFlags = messageFlags;

		this.type = type;
	}

	public abstract SpdtpMessageBase createResponse(byte additionalFlags = 0);
	
	public abstract SpdtpMessageBase createResendRequest(byte additionalFlags = 0);

	public abstract byte[] getBytes();

	public abstract SpdtpMessageBase setFromBytes(byte[] bytes);

	public virtual bool validate()
	{
		return /*(byte) (messageFlags & 0b0001_1100) == 0 &&*/ type == (byte) (messageFlags & 0b1100_0000) && isValid;
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

	public byte getKeepAliveFlag()
	{
		return (byte) (messageFlags & 0b0010_0000);
	}

	public static SpdtpMessageBase newMessageFromBytes(byte[] bytes)
	{
		try
		{
			if (bytes.Length == 4)
			{
				return new SpdtpNegotiationMessage().setFromBytes(bytes);
			}

			if (bytes.Length <= 70 && (bytes[0] & RESOURCE_SEGMENT) != RESOURCE_SEGMENT)
			{
				return new SpdtpResourceInfoMessage().setFromBytes(bytes);
			}

			return new SpdtpResourceSegment().setFromBytes(bytes);
		}
		catch (Exception ex) // Should never happen...
		{
			return null;
		}
	}
}