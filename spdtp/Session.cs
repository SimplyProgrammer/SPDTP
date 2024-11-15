using System;
using static SpdtpMessage;

/**
* This class represents the established session where the real communication is handled...
*/
public class Session
{
	public static readonly String TEXT_MSG_MARK = new String(new char[] {(char) 1, (char) 3, (char) 2,});

	protected SpdtpNegotiationMessage metadata;

	protected SpdtpConnection connection;

	protected Dictionary<int, ResourceTransmission> transmissions = new Dictionary<int, ResourceTransmission>();

	protected SpdtpResourceInfoMessage pendingResourceInfoMessage;

	public Session(SpdtpConnection connection, SpdtpNegotiationMessage openingMetadata)
	{
		setMetadata(openingMetadata);
		this.connection = connection;
	}

	public SpdtpResourceInfoMessage sendResource(byte[] resourceBytes, Object resourceDescriptor)
	{
		if (resourceDescriptor is FileStream)
		{
			pendingResourceInfoMessage = new SpdtpResourceInfoMessage(STATE_REQUEST, (resourceBytes.Length - 1) / metadata.getSegmentPayloadSize() + 1, Utils.truncString(((FileStream) resourceDescriptor).Name, 60));

			connection.sendMessageAsync(pendingResourceInfoMessage);
			return pendingResourceInfoMessage;
		}

		if (resourceDescriptor is String)
		{
			pendingResourceInfoMessage = resourceBytes.Length <= 60 ? 
				new SpdtpResourceInfoMessage(STATE_REQUEST, 0, resourceDescriptor.ToString()) :
				new SpdtpResourceInfoMessage(STATE_REQUEST, (resourceBytes.Length - 1) / metadata.getSegmentPayloadSize() + 1, TEXT_MSG_MARK + Utils.truncString(resourceDescriptor.ToString(), 12, ""));

			connection.sendMessageAsync(pendingResourceInfoMessage);
			return pendingResourceInfoMessage;
		}

		return null;
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
			else if (connection.attemptResend(pendingResourceInfoMessage))
				Console.WriteLine("Resend performed!");
			return true;
		}

		if (incomingResourceMsg.isState(STATE_REQUEST))
		{
			// TODO - initialize resources
		}

		if (incomingResourceMsg.isState(STATE_RESPONSE))
		{
			// TODO - start transmission

			pendingResourceInfoMessage = null;
			connection.resetResendAttempts();
			return true;
		}

		if (incomingResourceMsg.isState(STATE_RESEND_REQUEST))
		{
			if (pendingResourceInfoMessage == null)
				Console.WriteLine("No resource into to resend...");
			else
				connection.attemptResend(pendingResourceInfoMessage);
			return true;
		}

		return false;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		var transmission = transmissions[resourceSegment.getResourceIdentifier()];
		return transmission != null && transmission.handleResourceSegmentMsg(resourceSegment);
	}

	public SpdtpNegotiationMessage getMetadata()
	{
		return metadata;
	}

	public void setMetadata(SpdtpNegotiationMessage newMetadata)
	{
		metadata = newMetadata;
	}
}