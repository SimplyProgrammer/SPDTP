using System;

public class Session
{
	protected short segmentPayloadSize;

	public Session(short segmentPayloadSize)
	{
		setSegmentPayloadSize(segmentPayloadSize);
	}

	public short getSegmentPayloadSize()
	{
		return segmentPayloadSize;
	}

	public void setSegmentPayloadSize(short newSegmentPayloadSize)
	{
		segmentPayloadSize = newSegmentPayloadSize;
	}

	
}