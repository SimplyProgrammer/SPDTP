using System;
using NullFX.CRC;

public class SpdtpResourceSegment : SpdtpMessage
{
	public static readonly int TRANSMISSION_SUCCESSFUL_24x1 = 0b0000_0000__1111_1111__1111_1111__1111_1111;

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
			return GetType().Name + "[" + messageFlags + ", " + segmentID + ", " + resourceIdentifier + " | " + payload.Length + " bytes]";
		return GetType().Name + "[" + messageFlags + ", " + segmentID + ", " + resourceIdentifier + "]";
	}

	public override SpdtpResourceSegment createResponse(byte additionalFlags = 0)
	{
		return new SpdtpResourceSegment((byte) (STATE_RESPONSE | additionalFlags));
	}

	public override SpdtpResourceSegment createResendRequest(byte additionalFlags = 0)
	{
		return new SpdtpResourceSegment((byte) (STATE_RESEND_REQUEST | additionalFlags), getSegmentID(), getResourceIdentifier());
	}

	public override byte[] getBytes()
	{
		bool hasPayload = payload != null && payload.Length > 0;
		byte[] bytes = new byte[hasPayload ? 8+4+payload.Length : 8+2];

		bytes[0] = getMessageFlags();

		Buffer.BlockCopy(Utils.getBytes(segmentID), 1, bytes, 1, 3);

		Buffer.BlockCopy(Utils.getBytes(resourceIdentifier), 0, bytes, 4, 4);

		if (hasPayload)
		{
			Buffer.BlockCopy(payload, 0, bytes, 8, payload.Length);

			var crc32 = Crc32.ComputeChecksum(bytes, 0, bytes.Length-4);
			Buffer.BlockCopy(Utils.getBytes((int) crc32), 0, bytes, bytes.Length-4, 4);
			return bytes;
		}

		var crc16 = Crc16.ComputeChecksum(Crc16Algorithm.Standard, bytes, 0, bytes.Length-2);
		Buffer.BlockCopy(Utils.getBytes(crc16), 0, bytes, bytes.Length-2, 2);
		return bytes;
	}

	public override SpdtpMessage setFromBytes(byte[] bytes)
	{
		messageFlags = bytes[0];

		setSegmentID(Utils.getInt24(bytes, 1));

		setResourceIdentifier(Utils.getInt(bytes, 4));

		if (bytes.Length > 10) // Has payload
		{
			payload = new byte[bytes.Length-8-4];
			Buffer.BlockCopy(bytes, 8, payload, 0, payload.Length);

			// Console.WriteLine(Crc32.ComputeChecksum(bytes, 0, bytes.Length-4) + ", " + (uint) Utils.getInt(bytes, bytes.Length-4));
			isValid = Crc32.ComputeChecksum(bytes, 0, bytes.Length-4) == (uint) Utils.getInt(bytes, bytes.Length-4);
			return this;
		}

		isValid = Crc16.ComputeChecksum(Crc16Algorithm.Standard, bytes, 0, bytes.Length-2) == Utils.getShort(bytes, bytes.Length-2);
		return this;
	}

	public int getSegmentID()
	{
		return segmentID;
	}

	public void setSegmentID(int segmentID)
	{
		this.segmentID = segmentID;
	}

	public int getResourceIdentifier()
	{
		return resourceIdentifier;
	}

	public void setResourceIdentifier(int resourceIdentifier)
	{
		this.resourceIdentifier = resourceIdentifier;
	}

	public byte[] getPayload()
	{
		return payload;
	}

	public void setPayload(byte[] payload)
	{
		this.payload = payload;
	}

	public static SpdtpResourceSegment newTransmissionSuccessfulResponse(int resourceIdentifier, byte additionalFlags = 0) // 8x24
	{
		return new SpdtpResourceSegment((byte) (STATE_RESPONSE | additionalFlags), TRANSMISSION_SUCCESSFUL_24x1, resourceIdentifier);
	}
}