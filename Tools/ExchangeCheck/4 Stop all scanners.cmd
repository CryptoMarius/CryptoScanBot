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
rem  BOTH USER INTERFACES. The Avalonia scanner is CryptoScanBot.exe and the
rem  Photino one is CryptoScanBot.Photino.exe, and a night can hold a mix of the
rem  two. This is the only stop file; it used to have a Photino twin, which meant
rem  stopping a mixed night took two double clicks and left half the scanners
rem  running when you forgot one.
rem
rem  Named one by one and NOT as CryptoScanBot*.exe: that wildcard also matches
rem  CryptoScanBot.Emulator.exe, and a backtest that has been running for hours
rem  would be closed along with the scanners.
rem
rem  This stops EVERY scanner of both builds on this machine: they all share one
rem  process name per build, so they cannot be told apart from here. Close a
rem  single exchange from its own window instead.
rem
rem  Seconds to give the scanners to finish before the reports are built. A
rem  scanner writes its candles on the way out, so give it room.
set "WAIT=120"
rem ==========================================================================

title Stop all scanners

rem  Second heap snapshot and the comparison, BEFORE the scanners are asked to
rem  close - a stopped process has no heap to look at. Skips itself in silence
rem  when the start script did not take a first one. Keep HEAP_EXCHANGE equal to
rem  the one in the start script you used, "3 Start all scanners.cmd" or
rem  "5 Start all scanners (Photino).cmd".
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
rem
rem  CHECK THIS AGAINST TONIGHT'S SELECTION. Not every scanner is started every night any more -
rem  some nights are the spot markets, some nights the perpetual ones - and the snapshot simply
rem  finds no process when the name below is not among them. It throws in its own window, which
rem  then closes, so nothing tells you afterwards that the comparison never happened. That is how
rem  it stood on Kucoin Perpetual until 02-09-2026 while the spot markets were the ones running.
rem
rem  Binance Perpetual since 02-09-2026, the candidate the night of 01/02-09-2026 pointed at:
rem  +233 MB over 8,7 hours, ending on the highest reading of the night and giving none of it
rem  back, and with 449 signals so little actual work that whatever stays behind stands out
rem  cleanly.
set "HEAP_EXCHANGE=Binance Perpetual"
if not "%HEAP_EXCHANGE%"=="" (
    echo Second heap snapshot of %HEAP_EXCHANGE% ...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0heap-diff.ps1" -Mode Compare -Exchange "%HEAP_EXCHANGE%"
    echo.
)

echo Asking the scanners to close...
echo.

rem  Per build. taskkill sets errorlevel 1 when it found nothing, so a build that
rem  was not started this night says so in one line instead of printing "ERROR:
rem  The process ... not found" - which happens on every mixed night and reads
rem  like something went wrong. Only the error stream is swallowed; the SUCCESS
rem  line per closed scanner stays, so you can count them.
for %%A in (CryptoScanBot.exe CryptoScanBot.Photino.exe) do (
    taskkill /IM %%A 2>nul
    if errorlevel 1 echo   %%A - none running
)

echo.
echo Waiting %WAIT% seconds for them to finish (press a key to skip the wait)...
rem  Without /nobreak a key press - the spacebar, say - cuts the wait short, for
rem  when the windows are visibly gone before the timer runs out.
timeout /t %WAIT%

echo.
echo Still running (should be empty):
tasklist /FI "IMAGENAME eq CryptoScanBot.exe"
tasklist /FI "IMAGENAME eq CryptoScanBot.Photino.exe"

echo.
echo If anything is still listed above, close it from its own window before
echo reading the reports - its last candles are not on disk yet.
echo.
echo The memory sampling window does not stop by itself, you can close it now.
echo.
pause

call "%~dp02 Make reports.cmd"
