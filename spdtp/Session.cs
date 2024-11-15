using System;
using static SpdtpMessage;

/**
* This class represents the established session where the real communication is handled...
*/
public class Session
{
	protected short segmentPayloadSize;

	protected SpdtpConnection connection;

	protected Dictionary<int, ResourceTransmission> transmissions;

	protected SpdtpResourceInfoMessage pendingResourceInfoMessage;
	protected int infoMessageResendAttempts = 0;

	public Session(SpdtpConnection connection, short segmentPayloadSize)
	{
		setSegmentPayloadSize(segmentPayloadSize);
		this.connection = connection;
	}

	public SpdtpResourceInfoMessage sendResource(byte[] resourceBytes, Object resourceDescriptor)
	{
		if (resourceDescriptor is FileStream)
		{
			var resourceInfoMsg = new SpdtpResourceInfoMessage(STATE_REQUEST, (resourceBytes.Length - 1) / segmentPayloadSize + 1, Utils.truncString(((FileStream) resourceDescriptor).Name, 60));
			connection.sendMessageAsync(resourceInfoMsg);
			return resourceInfoMsg;
		}

		if (resourceDescriptor is String)
		{
			var resourceInfoMsg = new SpdtpResourceInfoMessage(STATE_REQUEST, resourceBytes.Length <= 60 ? 0 : (resourceBytes.Length - 1) / segmentPayloadSize + 1, Utils.truncString(resourceDescriptor.ToString(), 60, ""));
			connection.sendMessageAsync(resourceInfoMsg);
			return resourceInfoMsg;
		}

		return null;
	}

	protected void attemptResendPendingResourceInfo()
	{
		if (infoMessageResendAttempts++ > 2)
			connection.doTerminate("Session and connection terminated (too many transmission errors)!");
		else
			connection.sendMessageAsync(pendingResourceInfoMessage);
	}

	public bool handleIncomingResourceMsg(SpdtpResourceInfoMessage incomingResourceMsg)
	{
		if (!incomingResourceMsg.validate())
		{
			Console.WriteLine("Erroneous incoming resource info was received: " + incomingResourceMsg + "!");

			if (pendingResourceInfoMessage == null)
			{
				connection.sendMessageAsync(incomingResourceMsg.createResendRequest());
				Console.WriteLine("Resend requested!");
			}
			else
			{
				attemptResendPendingResourceInfo();
				Console.WriteLine("Resend performed!");
			}
			return true;
		}

		if (incomingResourceMsg.isState(STATE_REQUEST))
		{
			// TODO
		}

		if (incomingResourceMsg.isState(STATE_RESPONSE))
		{
			// TODO
		}

		if (incomingResourceMsg.isState(STATE_RESEND_REQUEST))
		{
			if (pendingResourceInfoMessage == null)
				Console.WriteLine("No resource into to resend...");
			else
				attemptResendPendingResourceInfo();
			return true;
		}

		return false;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		var transmission = transmissions[resourceSegment.getResourceIdentifier()];
		if (transmission != null)
		{
			transmission.handleResourceSegmentMsg(resourceSegment);
			return true;
		}

		return false;
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