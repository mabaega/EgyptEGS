#!/bin/bash

echo "Installing Egypt EGS Token Service..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

SERVICE_NAME=egyptegs
WORKING_DIR=$(dirname "$(readlink -f "$0")")
EXEC_PATH="$WORKING_DIR/SignServices"

# Create systemd service file
cat > /etc/systemd/system/$SERVICE_NAME.service << EOF
[Unit]
Description=Egypt EGS Token Service
After=network.target

[Service]
Type=notify
WorkingDirectory=$WORKING_DIR
ExecStart=$EXEC_PATH
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd, enable and start service
systemctl daemon-reload
systemctl enable $SERVICE_NAME
systemctl start $SERVICE_NAME

echo "Service installation complete."