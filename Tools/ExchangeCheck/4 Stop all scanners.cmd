@echo off
setlocal

rem ==========================================================================
rem  Asks every running scanner to close, waits for them to finish, and then
rem  builds the reports.
rem
rem  NOTE: taskkill WITHOUT /F is a request, not a kill. It posts a close
rem  message to the window - the same thing as clicking the cross - and the
rem  scanner then runs its own shutdown: the candles still in memory are
rem  written, the settings are saved and the data folder lock is released.
rem  Adding /F would cut all of that off halfway, so do not add it. Anything
rem  that is still running below has to be closed by hand.
rem
rem  This stops EVERY CryptoScanBot.exe on this machine: they all share one
rem  process name, so they cannot be told apart from here. Close a single
rem  exchange from its own window instead.
rem
rem  Seconds to give the scanners to finish before the reports are built. A
rem  scanner writes its candles on the way out, so give it room.
set "WAIT=120"
rem ==========================================================================

title Stop all scanners

echo Asking the scanners to close...
echo.

taskkill /IM CryptoScanBot.exe

echo.
echo Waiting %WAIT% seconds for them to finish...
timeout /t %WAIT% /nobreak >nul

echo.
echo Still running (should be empty):
tasklist /FI "IMAGENAME eq CryptoScanBot.exe"

echo.
echo If anything is still listed above, close it from its own window before
echo reading the reports - its last candles are not on disk yet.
echo.
echo The memory sampling window does not stop by itself, you can close it now.
echo.
pause

call "%~dp02 Make reports.cmd"
