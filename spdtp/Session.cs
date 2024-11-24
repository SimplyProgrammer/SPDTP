using System;

using static SpdtpMessageBase;
using static SpdtpResourceInfoMessage;
using static SpdtpResourceSegment;
using static ResourceTransmission;

/**
* This class represents the established session where the real communication is handled...
*/
public class Session : SessionBase<SpdtpNegotiationMessage, SpdtpMessageBase, bool>
{
	protected Dictionary<int, ResourceTransmission> transmissions = new Dictionary<int, ResourceTransmission>();

	protected SpdtpResourceInfoMessage pendingResourceInfoMessage;

	public Session(Connection connection, SpdtpNegotiationMessage metadata) : base(connection, metadata) {}

	// protected void handlePendingResourceInfoTimeout(AsyncTimer resender)
	// {
	// 	Console.WriteLine(pendingResourceInfoMessage.ToString() + " - Pending resource info timed out, transmission aborted!");
	// 	Console.WriteLine(transmissions.Remove(pendingResourceInfoMessage.getResourceIdentifier()) ? "Resources deallocated!" : "Resources not present (already deallocated)!");
	// }

	public override void onKeepAlive()
	{
		if (pendingResourceInfoMessage != null)
			connection.sendMessage(pendingResourceInfoMessage/*, 2, 5000, err*/);

		foreach (var entry in transmissions)
			entry.Value?.onKeepAlive();
	}

	public SpdtpResourceInfoMessage sendResource(byte[] resourceBytes, Object resourceDescriptor)
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

			// 	connection.sendMessage(pendingResourceInfoMessage, 0, 0, err).setOnStopCallback(handlePendingResourceInfoTimeout);
			// 	return pendingResourceInfoMessage;
			// }
			
			segmentCount = (resourceBytes.Length - 1) / metadata.getSegmentPayloadSize() + 1;
			pendingResourceInfoMessage = new SpdtpResourceInfoMessage(STATE_REQUEST, segmentCount, TEXT_MSG_MARK + Utils.truncString(resourceDescriptor.ToString(), 12, ""));
		}
		else
		{
			Console.WriteLine(resourceDescriptor?.GetType().Name + " is unsupported!");
			return null;
		}

		try
		{
			var transmission = new ResourceTransmission(connection, pendingResourceInfoMessage, new SpdtpResourceSegment[segmentCount]);
			transmissions.Add(pendingResourceInfoMessage.getResourceIdentifier(), transmission);
			transmission.initializeResourceTransmission(resourceBytes);

			connection.sendMessage(pendingResourceInfoMessage/*, 2, 5000, err*/);

			Console.WriteLine("Informing the other peer about incoming: " + pendingResourceInfoMessage.ToString(true) + "!");
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine("Resource for " + pendingResourceInfoMessage.ToString(true) + " was already initialized, waiting for transmission approval!");
		}

		return pendingResourceInfoMessage;
	}

	public override bool handleIncomingMessage(SpdtpMessageBase message)
	{
		if (message is SpdtpResourceInfoMessage)
			return handleIncomingResourceMsg((SpdtpResourceInfoMessage) message);

		if (message is SpdtpResourceSegment)
			return handleResourceSegmentMsg((SpdtpResourceSegment) message);
		return false;
	}

	public bool handleIncomingResourceMsg(SpdtpResourceInfoMessage incomingResourceMsg)
	{
		if (!incomingResourceMsg.validate())
		{
			Console.WriteLine("Erroneous incoming resource info was received: " + incomingResourceMsg + "!");

			if (pendingResourceInfoMessage == null)
			{
				connection.sendMessage(incomingResourceMsg.createResendRequest());
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

				Console.WriteLine("Resource for " + incomingResourceMsg.ToString(true) + " were initialized successfully, ready for incoming transmission!");
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine("Resource for " + incomingResourceMsg.ToString(true) + " was already initialized, waiting for incoming transmission!");
			}

			connection.sendMessage(incomingResourceMsg.createResponse()/*, 0, 0, connection is CliPeer && ((CliPeer) connection)._testingResponseErrorCount-- > 0*/);
			return true;
		}

		if (incomingResourceMsg.isState(STATE_RESPONSE))
		{
			var transmission = transmissions.GetValueOrDefault(incomingResourceMsg.getResourceIdentifier());
			if (transmission == null)
			{
				Console.WriteLine("Unable to initiate transmission for " + incomingResourceMsg.ToString(true) + "! Resources were not allocated!");
				return true;
			}

			Console.WriteLine("Initiated transmission of " + incomingResourceMsg.ToString(true) + "!");
			transmission.setExpectedSegmentCount(incomingResourceMsg.getSegmentCount());
			transmission.start();

			pendingResourceInfoMessage = null; // "House keeping..."
			connection.resetResendAttempts();
			return true;
		}

		if (incomingResourceMsg.isState(STATE_RESEND_REQUEST))
		{
			if (pendingResourceInfoMessage == null)
				Console.WriteLine("No resource info to resend...");
			else
				connection.attemptResend(pendingResourceInfoMessage);
			return true;
		}

		return false;
	}

	public bool handleResourceSegmentMsg(SpdtpResourceSegment resourceSegment)
	{
		int resourceIdentifier = resourceSegment.getResourceIdentifier();
		var transmission = transmissions.GetValueOrDefault(resourceIdentifier);
		if (resourceSegment.getSegmentID() == TRANSMISSION_SUCCESSFUL_24x1)
		{
			// transmission.finalize();
			if (transmissions.Remove(resourceIdentifier)) // ? Maybe do not for caching
			{
				transmission.stop();
				Console.WriteLine("Resource (" + resourceIdentifier + ") was successfully received by the other peer, resources deallocated!");
			}
			return true;
		}

		if (transmission != null)
		{
			int status = transmission.handleIncomingMessage(resourceSegment);
			if (status >= FINISHED)
			{
				connection.sendMessage(newTransmissionSuccessfulResponse(resourceIdentifier));
				connection.handleTransmittedResource(transmission);
				transmissions.Remove(resourceIdentifier);
			}
			return status > UNPROCESSED;
		}

		return false;
	}

	public Dictionary<int, ResourceTransmission> getTransmissions()
	{
		return transmissions;
	}
}