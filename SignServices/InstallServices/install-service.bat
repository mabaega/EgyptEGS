@echo off
echo Installing Egypt EGS Token Service...

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Please run as Administrator
    pause
    exit /b 1
)

set SERVICE_NAME=EgyptEGS
set DISPLAY_NAME=Egypt EGS Token Service
set DESCRIPTION="Token signing service for Egypt EGS integration"
set BIN_PATH="\"%~dp0SignServices.exe\""

echo Checking for existing service...
sc query %SERVICE_NAME% >nul 2>&1
if %errorLevel% equ 0 (
    echo Found existing service. Removing...
    sc stop %SERVICE_NAME% >nul 2>&1
    sc delete %SERVICE_NAME%
    timeout /t 2 /nobreak >nul
)

echo Installing service from: %BIN_PATH%
sc create %SERVICE_NAME% binPath= %BIN_PATH% start= auto DisplayName= "%DISPLAY_NAME%"
sc description %SERVICE_NAME% %DESCRIPTION%
sc start %SERVICE_NAME%

echo Service installation complete.
pause