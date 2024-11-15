using System;
using static SpdtpMessage;

/**
* 
*/
public class ResourceTransmission
{
	protected SpdtpConnection connection;

	protected int successfullyReceivedCount;
	protected SpdtpResourceSegment[] segments;

	public ResourceTransmission(SpdtpConnection connection, SpdtpResourceSegment[] segments)
	{
		this.connection = connection;
		this.segments = segments;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		
		return false;
	}
}