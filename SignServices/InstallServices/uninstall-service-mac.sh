#!/bin/bash

echo "Uninstalling Egypt EGS Token Service..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

SERVICE_NAME=com.egyptegs.signing

# Unload service
launchctl unload /Library/LaunchDaemons/$SERVICE_NAME.plist

# Remove plist file
rm -f /Library/LaunchDaemons/$SERVICE_NAME.plist

echo "Service uninstallation complete."