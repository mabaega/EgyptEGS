#!/bin/bash

echo "Installing Egypt EGS Token Service..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

SERVICE_NAME=com.egyptegs.signing
WORKING_DIR=$(dirname "$(readlink -f "$0")")
EXEC_PATH="$WORKING_DIR/SignServices"

# Create launchd plist file
cat > /Library/LaunchDaemons/$SERVICE_NAME.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>$SERVICE_NAME</string>
    <key>WorkingDirectory</key>
    <string>$WORKING_DIR</string>
    <key>ProgramArguments</key>
    <array>
        <string>$EXEC_PATH</string>
    </array>
    <key>KeepAlive</key>
    <true/>
    <key>RunAtLoad</key>
    <true/>
    <key>StandardErrorPath</key>
    <string>/var/log/$SERVICE_NAME.err</string>
    <key>StandardOutPath</key>
    <string>/var/log/$SERVICE_NAME.log</string>
</dict>
</plist>
EOF

# Load and start service
launchctl load /Library/LaunchDaemons/$SERVICE_NAME.plist

echo "Service installation complete."