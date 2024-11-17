using System;
using System.Net.Http.Headers;
using static SpdtpMessage;
using static SpdtpResourceInfoMessage;

/**
* This class represents the established session where the real communication is handled...
*/
public class Session
{
	protected SpdtpNegotiationMessage metadata;

	protected Connection connection;

	protected Dictionary<int, ResourceTransmission> transmissions = new Dictionary<int, ResourceTransmission>();

	protected SpdtpResourceInfoMessage pendingResourceInfoMessage;
	protected AsyncTimer pendingResourceInfoResender;

	public Session(Connection connection, SpdtpNegotiationMessage openingMetadata)
	{
		setMetadata(openingMetadata);
		this.connection = connection;
	}

	protected void handlePendingResourceInfoTimeout(AsyncTimer resender)
	{
		Console.WriteLine(pendingResourceInfoMessage.ToString() + " - Pending resource info timed out, transmission aborted!");
		Console.WriteLine(transmissions.Remove(pendingResourceInfoMessage.getResourceIdentifier()) ? "Resources deallocated!" : "Resources not present (already deallocated)!");
	}

	public SpdtpResourceInfoMessage sendResource(byte[] resourceBytes, Object resourceDescriptor, bool err = false)
	{
		if (pendingResourceInfoMessage != null)
		{
			Console.WriteLine(pendingResourceInfoMessage + " is pending, please wait before sending another!");
			return null;
		}

		int segmentCount;
		if (resourceDescriptor is FileStream)
		{
			segmentCount = (resourceBytes.Length - 1) / metadata.getSegmentPayloadSize() + 1;
			pendingResourceInfoMessage = new SpdtpResourceInfoMessage(STATE_REQUEST, segmentCount, Utils.truncString(((FileStream) resourceDescriptor).Name, 64));
		}
		else if (resourceDescriptor is String)
		{
			// if (resourceBytes.Length <= 64) // Send textual msg directly without fragmentation if 64 chars or less...
			// {
			// 	pendingResourceInfoMessage = new SpdtpResourceInfoMessage(STATE_REQUEST, 0, resourceDescriptor.ToString());

			// 	connection.sendMessageAsync(pendingResourceInfoMessage, 0, 0, err).setOnStopCallback(handlePendingResourceInfoTimeout);
			// 	return pendingResourceInfoMessage;
			// }
			
			segmentCount = (resourceBytes.Length - 1) / metadata.getSegmentPayloadSize() + 1;
			pendingResourceInfoMessage = new SpdtpResourceInfoMessage(STATE_REQUEST, segmentCount, TEXT_MSG_MARK + Utils.truncString(resourceDescriptor.ToString(), 12, ""));
		}
		else
		{
			Console.WriteLine(resourceDescriptor?.GetType() + " is unsupported!");
			return null;
		}

		try
		{
			var transmission = new ResourceTransmission(connection, pendingResourceInfoMessage, new SpdtpResourceSegment[segmentCount]);
			transmissions.Add(pendingResourceInfoMessage.getResourceIdentifier(), transmission);
			transmission.initializeResourceTransmission(resourceBytes, metadata.getSegmentPayloadSize());

			pendingResourceInfoResender = connection.sendMessageAsync(pendingResourceInfoMessage, 2, 5000, err).setOnStopCallback(handlePendingResourceInfoTimeout);

			Console.WriteLine("Informing the other peer about incoming: " + pendingResourceInfoMessage.ToString(true) + "!");
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine("Resource for " + pendingResourceInfoMessage.ToString(true) + " was already initialized, waiting for transmission approval!");
		}

		return pendingResourceInfoMessage;
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
			try
			{
				var transmission = new ResourceTransmission(connection, incomingResourceMsg, new SpdtpResourceSegment[incomingResourceMsg.getSegmentCount()]);
				transmissions.Add(incomingResourceMsg.getResourceIdentifier(), transmission);

				Console.WriteLine("Resource for " + pendingResourceInfoMessage.ToString(true) + " were initialized successfully, ready for incoming transmission!");
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine("Resource for " + pendingResourceInfoMessage.ToString(true) + " was already initialized, waiting for incoming transmission!");
			}

			connection.sendMessageAsync(incomingResourceMsg.createResponse());
			return true;
		}

		if (incomingResourceMsg.isState(STATE_RESPONSE))
		{
			var transmission = transmissions[incomingResourceMsg.getResourceIdentifier()];
			if (transmission == null)
			{
				Console.WriteLine("Unable to initiate transmission for " + incomingResourceMsg.ToString(true) + "! Resources were not allocated!");
				return true;
			}

			transmission.setExpectedSegmentCount(incomingResourceMsg.getSegmentCount());
			transmission.initiateTransmission();
			Console.WriteLine("Transmission of " + incomingResourceMsg.ToString(true) + " initiated!");

			pendingResourceInfoMessage = null; // "House keeping..."
			pendingResourceInfoResender?.stop(false);
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