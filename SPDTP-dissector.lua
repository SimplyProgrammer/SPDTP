spdtpProto = Proto("SPDTP", "Simple Peer 2 peer Data Transfer Protocol (SPDTP)")

local messageTypeMap = {
	[0] = "UNKNOWN",
	[1] = "NEGOTIATION",
	[2] = "INCOMING_RESOURCE_INFO",
	[3] = "RESOURCE_SEGMENT"
}

local transmissionStateMap = {
	[0] = "STATE_RESEND_REQUEST",
	[1] = "STATE_REQUEST",
	[2] = "STATE_RESPONSE"
	--[3] = "STATE_SUCCESS"
}

-- GENERAL
local messageFlagsField = ProtoField.uint8("spdtp.flags", "Message flags", base.DEC)
local messageTypeField = ProtoField.uint8("spdtp.message_type", "Message type", base.DEC, messageTypeMap, 0xC0)
local transmissionStateField = ProtoField.uint8("spdtp.transmission_state", "Transmission state", base.DEC, transmissionStateMap, 0x03)

local checksumField = ProtoField.uint16("spdtp.negotiation.checksum", "Checksum", base.DEC)

-- NEGOTIATION
local segmentPayloadSizeField = ProtoField.uint16("spdtp.negotiation.payload_size", "Segment payload size", base.DEC)

-- INCOMING_RESOURCE_INFO
local segmentCountField = ProtoField.uint24("spdtp.resource_info.segment_count", "Segment Count", base.DEC)

local resourceNameField = ProtoField.string("spdtp.resource_info.resource_name", "Resource Name", base.ASCII)

-- RESOURCE_SEGMENT
local segmentIdField = ProtoField.uint24("spdtp.segment.id", "Segment ID", base.DEC)
local resourceIdField = ProtoField.int32("spdtp.segment.resource_identifier", "Resource Identifier", base.DEC)

local payloadField = ProtoField.bytes("spdtp.segment.payload", "Payload")

spdtpProto.fields = {
	messageFlagsField, messageTypeField, transmissionStateField,
	segmentPayloadSizeField, checksumField,
	segmentCountField, resourceNameField,
	segmentIdField, resourceIdField, payloadField
}

function spdtpProto.dissector(buffer, pinfo, tree)
	pinfo.cols.protocol = spdtpProto.name

	local subtree = tree:add(spdtpProto, buffer(), "SPDTP Protocol")

	local flags = buffer(0, 1)
	subtree:add(messageFlagsField, flags)

	local flagsNum = flags:uint()
	local messageType = bit.rshift(flagsNum, 6)
	local transmissionState = bit.band(flagsNum, 3)

	if flagsNum == 255 then
		subtree:add_expert_info(PI_PROTOCOL, PI_WARN, "Session and connection terminated")
		pinfo.cols.info = "Session and connection terminated"
		return
	else
		subtree:add(messageTypeField, flags)
		subtree:add(transmissionStateField, flags)
	end

	local origInfo = tostring(pinfo.cols.info)
	pinfo.cols.info = string.format(
		"%s (%s - %s)",
		origInfo,
		messageTypeMap[messageType] or "Unknown",
		transmissionStateMap[transmissionState] or "Unknown"
	)

	if messageType == 1 then -- NEGOTIATION
		if buffer:len() < 4 then
			return
		end

		local segmentPayloadSize = buffer(1, 2)
		subtree:add(segmentPayloadSizeField, segmentPayloadSize)

		local checksum = buffer(3, 1)
		subtree:add(checksumField, checksum)

	elseif messageType == 2 then -- INCOMING_RESOURCE_INFO
		if buffer:len() < 8 then 
			return 
		end

		local segmentCount = buffer(1, 3):uint()
		subtree:add(segmentCountField, buffer(1, 3))
		
		local resourceNameLen = buffer:len() - 6 -- Excluding flags, segmentCount, checksum
		local resourceName = buffer(4, resourceNameLen):string()
		subtree:add(resourceNameField, buffer(4, resourceNameLen))

		local checksum = buffer(buffer:len() - 2, 2)
		subtree:add(checksumField, checksum)

	elseif messageType == 3 then -- RESOURCE_SEGMENT
		if buffer:len() < 10 then 
			return 
		end

		local segmentId = buffer(1, 3):uint()
		local resourceId = buffer(4, 4):int()
		subtree:add(segmentIdField, buffer(1, 3))
		subtree:add(resourceIdField, buffer(4, 4))

		if buffer:len() == 10 then -- No payload (No STATE_REQUEST)
			local checksum = buffer(8, 2)
			subtree:add(checksumField, checksum)
		else -- Payload present (STATE_REQUEST)		Excluding flags, segmentId, resourceId, checksum
			
			local payloadLen = buffer:len() - 12
			local payload = buffer(8, payloadLen)
			subtree:add(payloadField, payload)

			local checksum = buffer(buffer:len() - 4, 4)
			subtree:add(checksumField, checksum)
		end
	end
end

local udpPort = DissectorTable.get("udp.port")
for port = 18800, 18811 do
	udpPort:add(port, spdtpProto)
end