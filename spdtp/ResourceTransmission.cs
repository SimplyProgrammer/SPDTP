using System;
using static SpdtpMessage;

/**
* 
*/
public class ResourceTransmission
{
	protected SpdtpConnection connection;

	protected int currentSegmentCount, resourceIdentifier;
	protected SpdtpResourceSegment[] segments;

	public ResourceTransmission(SpdtpConnection connection, int resourceIdentifier, SpdtpResourceSegment[] segments)
	{
		this.connection = connection;
		this.segments = segments;
		this.resourceIdentifier = resourceIdentifier;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		
		return false;
	}
}