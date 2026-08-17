@echo off
setlocal

rem ==========================================================================
rem  Same as "4 Stop all scanners.cmd", but for the Photino build. Only
rem  CryptoScanBot.Photino.exe is asked to close; Avalonia scanners that happen
rem  to be running are left alone.
rem
rem  NOTE: taskkill WITHOUT /F is a request, not a kill. It posts a close
rem  message to the window - the same thing as clicking the cross - so the
rem  scanner writes its candles, saves its settings and releases the data
rem  folder lock. Do not add /F.
rem
rem  Seconds to give the scanners to finish before the reports are built.
set "WAIT=120"
rem ==========================================================================

title Stop all scanners (Photino)

echo Asking the scanners to close...
echo.

taskkill /IM CryptoScanBot.Photino.exe

echo.
echo Waiting %WAIT% seconds for them to finish...
timeout /t %WAIT% /nobreak >nul

echo.
echo Still running (should be empty):
tasklist /FI "IMAGENAME eq CryptoScanBot.Photino.exe"

echo.
echo If anything is still listed above, close it from its own window before
echo reading the reports - its last candles are not on disk yet.
echo.
echo The memory sampling window does not stop by itself, you can close it now.
echo.
pause

call "%~dp02 Make reports.cmd"
