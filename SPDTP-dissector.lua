spdtpProto = Proto("SPDTP", "Simple Peer 2 peer Data Transfer Protocol (SPDTP)")

local messageTypeMap = 
{
	[0] = "UNKNOWN",
	[1] = "NEGOTIATION",
	[2] = "INCOMING_RESOURCE_INFO",
	[3] = "RESOURCE_SEGMENT"
}

local transmissionStateMap = 
{
	[0] = "STATE_RESEND_REQUEST",
	[1] = "STATE_REQUEST",
	[2] = "STATE_RESPONSE"
}

local messageFlagsField = ProtoField.uint8("spdtp.flags", "Message flags", base.DEC)
local messageTypeField = ProtoField.uint8("spdtp.message_type", "Message type", base.DEC, messageTypeMap, 0xC0)
local transmissionStateField = ProtoField.uint8("spdtp.transmission_state", "Transmission state", base.DEC, transmissionStateMap, 0x03)

local segmentPayloadSizeField = ProtoField.uint16("spdtp.payload_size", "Segment payload size", base.DEC)

local checksumField = ProtoField.uint8("spdtp.checksum", "Checksum", base.DEC)

spdtpProto.fields = { messageFlagsField, messageTypeField, transmissionStateField, segmentPayloadSizeField, checksumField }

function spdtpProto.dissector(buffer, pinfo, tree)
	if buffer:len() < 4 then
		return
	end

	pinfo.cols.protocol = spdtpProto.name

	local subtree = tree:add(spdtpProto, buffer(), "SPDTP Protocol")

	local flags = buffer(0, 1)
	subtree:add(messageFlagsField, flags)

	local flagsNum = flags:uint()
	if flagsNum == 255 then
		subtree:add_expert_info(PI_PROTOCOL, PI_WARN, "Session and connection terminated")
		pinfo.cols.info = string.format("%s | Session and connection terminated", original_info)
	else
		subtree:add(messageTypeField, flags)
		subtree:add(transmissionStateField, flags)

		local origInfo = tostring(pinfo.cols.info)
		pinfo.cols.info = string.format(
			"%s (%s - %s)",
			origInfo,
			messageTypeMap[messageType] or "Unknown",
			transmissionStateMap[transmissionState] or "Unknown"
		)
	end
	
	local payload_size = buffer(1, 2)
	subtree:add(segmentPayloadSizeField, payload_size)

	local checksum = buffer(3, 1)
	subtree:add(checksumField, checksum)

	local messageType = bit.rshift(flagsNum, 6)
	local transmissionState = bit.band(flagsNum, 3)

end

local udp_port = DissectorTable.get("udp.port")
for port = 8800, 8811 do
	udp_port:add(port, spdtpProto)
end