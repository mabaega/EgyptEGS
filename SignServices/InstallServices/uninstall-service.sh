#!/bin/bash

echo "Uninstalling Egypt EGS Token Service..."

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "Please run as root (use sudo)"
    exit 1
fi

SERVICE_NAME=egyptegs

# Stop and disable service
systemctl stop $SERVICE_NAME
systemctl disable $SERVICE_NAME

# Remove service file
rm -f /etc/systemd/system/$SERVICE_NAME.service

# Reload systemd
systemctl daemon-reload

echo "Service uninstallation complete."