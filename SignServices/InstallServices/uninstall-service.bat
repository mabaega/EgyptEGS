@echo off
echo Uninstalling Egypt EGS Token Service...

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Please run as Administrator
    pause
    exit /b 1
)

set SERVICE_NAME=EgyptEGS

sc stop %SERVICE_NAME%
sc delete %SERVICE_NAME%

echo Service uninstallation complete.
pause