using System;
using NullFX.CRC;

// public class SpdtpResourceSegment : SpdtpMessage
// {
// 	public override string ToString()
// 	{
// 		return GetType().Name + "[" + messageFlags + ", " + segmentCount + "]";
// 	}

// 	public override SpdtpResourceSegment createResponse(byte additionalFlags = 0)
// 	{
// 		return new SpdtpResourceSegment((byte) (INCOMING_RESOURCE_INFO | getKeepAliveFlag() | STATE_RESPONSE | additionalFlags));
// 	}

// 	public override SpdtpResourceSegment createResendRequest(byte additionalFlags = 0)
// 	{
// 		return new SpdtpResourceSegment((byte) (INCOMING_RESOURCE_INFO | getKeepAliveFlag() | STATE_RESEND_REQUEST | additionalFlags), 0);
// 	}

// 	public override byte[] getBytes()
// 	{
// 		byte[] bytes = new byte[66];

// 		return bytes;
// 	}

// 	public override SpdtpMessage setFromBytes(byte[] bytes)
// 	{

// 		return this;
// 	}
// }