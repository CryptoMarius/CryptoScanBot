@echo off
setlocal

rem ==========================================================================
rem  Deletes the CANDLE database of every supported exchange, so the next run
rem  fetches its history again from scratch.
rem
rem  That is the file next to the scanner database, named after the exchange
rem  itself ("Binance Spot.db", "Bybit EU Spot.db"), together with its -shm and
rem  -wal companions. It holds the candles and the Symbol / SymbolInterval
rem  bookkeeping that goes with them, and nothing else.
rem
rem  What stays: CryptoScanBot.db (symbols, signals, positions, zones), every
rem  settings file, and the logs. Use 9 Clear all databases.cmd for the first,
rem  7 Clear all logs.cmd for the last.
rem
rem  This is the reset to reach for after a fix that changes how candles are
rem  STORED - a tick size that was wrong, for instance. Every candle carries the
rem  scale it was written with, so old and repaired candles otherwise end up in
rem  the same series and only half of it is right.
rem
rem  Refetching is not free: the exchange only hands back what it still keeps
rem  per interval (Kraken Spot 720 candles and nothing before that), so a delete
rem  shortens the history of the deeper intervals for good.
rem
rem  Stop the scanners first (4 Stop all scanners.cmd). This file refuses to run
rem  while one is up: a database in use does not delete, and a half deleted one
rem  is worse than one that was left alone.
rem
rem  Change this line if your folders are somewhere else:
set "DATA=E:\CryptoScanBot\Data"
rem ==========================================================================

title Clear all candle databases

echo Candle databases are about to be deleted under:
echo   %DATA%
echo.
echo The scanner databases, the settings and the logs are NOT touched.
echo The next run fetches the candles again, which takes a while per exchange.
echo.

rem findstr and not find: on a machine with git for windows on the path, "find" is the unix one and
rem it does not understand this at all.
set "RUNNING="
tasklist /FI "IMAGENAME eq CryptoScanBot.exe" /NH 2>nul | findstr /I "CryptoScanBot" >nul && set "RUNNING=1"
tasklist /FI "IMAGENAME eq CryptoScanBot.Photino.exe" /NH 2>nul | findstr /I "CryptoScanBot" >nul && set "RUNNING=1"

if defined RUNNING (
    echo *** Scanners are still running. ***
    echo Stop them first, their databases are open and cannot be deleted.
    echo.
    pause
    exit /b 1
)

choice /C YN /N /M "Delete the candle databases? [Y/N] "
if errorlevel 2 goto :cancelled

echo.

set "FAILED="

call :candles Binance Spot
call :candles Binance Futures
call :candles BitMart Futures
call :candles Bitvavo Spot
call :candles BloFin Futures
call :candles Bybit Spot
call :candles Bybit Futures
call :candles "Bybit EU" Spot
call :candles Coinbase Spot
call :candles HyperLiquid Spot
call :candles HyperLiquid Futures
call :candles Kraken Spot
call :candles Kraken Futures
call :candles Kucoin Spot
call :candles Kucoin Futures
call :candles Mexc Spot
call :candles Mexc Futures
call :candles Okx Spot
call :candles Okx Futures

rem  Alpaca, same as in the start files - remove the rem to take it along:
rem call :candles Alpaca Spot

rem  The emulator folders (Binance\Emulator, Binance\Futures.Emulator) are left
rem  out on purpose: those hold a recorded run, not a live exchange, and
rem  refetching does not bring one back.

echo.
if defined FAILED (
    echo *** Not everything was deleted, see the lines above. ***
) else (
    echo Done.
)
echo.
pause
exit /b 0


rem --------------------------------------------------------------------------
rem  %1 = exchange folder, %2 = trading type folder. The candle database is
rem  named after the two together, which is how the scanner names it as well.
rem --------------------------------------------------------------------------
:candles
echo %~1 %~2
set "FOLDER=%DATA%\%~1\%~2"
set "FILE=%FOLDER%\%~1 %~2.db"

if not exist "%FOLDER%\" (
    echo    no data folder, skipped
    goto :eof
)
if not exist "%FILE%" (
    echo    no candle database, skipped
    goto :eof
)

del /f /q "%FILE%" "%FILE%-shm" "%FILE%-wal" 2>nul

if exist "%FILE%" (
    echo    *** FAILED - the file is still there, is something holding it open?
    set "FAILED=1"
) else (
    echo    deleted
)
goto :eof


:cancelled
echo.
echo Cancelled, nothing was deleted.
echo.
pause
exit /b 1
