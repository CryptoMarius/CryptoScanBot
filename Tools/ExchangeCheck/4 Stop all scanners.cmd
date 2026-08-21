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

rem  Second heap snapshot and the comparison, BEFORE the scanners are asked to
rem  close - a stopped process has no heap to look at. Skips itself in silence
rem  when the start script did not take a first one. Keep HEAP_EXCHANGE equal to
rem  the one in "3 Start all scanners.cmd".
rem  Which exchange the snapshots are taken of. Point this at the scanner you are actually
rem  investigating: a heap dump runs to 1.6 GB and two of them per exchange is why this is one
rem  name and not a list.
rem
rem  Kucoin Futures since 20-08-2026. It was Okx Futures, chosen because it showed the steepest
rem  memory growth of the night - but that number was measured over the WHOLE run and so it was
rem  mostly the one-off filling of the caches in the first hour. Over the last six hours Okx
rem  Futures was at -2,7 MB per hour, so there is nothing there to catch. Kucoin Futures (+9,1)
rem  and Bybit Spot (+6,8) were the only two still climbing after the warm-up; Kucoin Futures is
rem  the worse of the two and goes first.
set "HEAP_EXCHANGE=Kucoin Futures"
if not "%HEAP_EXCHANGE%"=="" (
    echo Second heap snapshot of %HEAP_EXCHANGE% ...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0heap-diff.ps1" -Mode Compare -Exchange "%HEAP_EXCHANGE%"
    echo.
)

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
