@echo off
setlocal

rem ==========================================================================
rem  Start this AFTER the scanners are running, then leave the window open.
rem  It writes one csv per scanner, named after that scanner's data folder.
rem  Closing the window stops the sampling - the scanners are not affected.
rem
rem  Change this line if you want the samples somewhere else:
set "SAMPLE_FOLDER=E:\CryptoScanBot\Data\Memory"
rem ==========================================================================

title Memory sampling - keep this window open

echo Samples are written to: %SAMPLE_FOLDER%
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0sample-process.ps1" -Out "%SAMPLE_FOLDER%"

echo.
echo Sampling has stopped.
pause
