#!/bin/bash

# Running this script with gitbash or other tool like that should install it into your WS...
# If this does not work, just do it manually.

mkdir -p ~/AppData/Roaming/Wireshark/plugins/ && cp -f ./SPDTP-dissector.lua $_