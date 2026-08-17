@echo off
setlocal

rem ==========================================================================
rem  Deletes the SCANNER database (CryptoScanBot.db) of every supported
rem  exchange, so the next run starts on an empty one.
rem
rem  That file holds everything the scanner itself keeps: the symbol list, the
rem  signals, the positions with their steps and trades, the zones and the
rem  account balances. None of it comes back - the exchange has no record of a
rem  signal the scanner produced, and a paper position exists nowhere else.
rem  This is the heavy one of the two; 8 Clear all candle databases.cmd throws
rem  away candles the exchange can hand out again.
rem
rem  What stays: the candle databases, every settings file, and the logs.
rem
rem  The scanner builds the tables again on startup (Database.CreateDatabase),
rem  so an empty folder is a valid starting point - it just knows nothing about
rem  what happened before.
rem
rem  Stop the scanners first (4 Stop all scanners.cmd). This file refuses to run
rem  while one is up: a database in use does not delete, and a half deleted one
rem  is worse than one that was left alone.
rem
rem  Change this line if your folders are somewhere else:
set "DATA=E:\CryptoScanBot\Data"
rem ==========================================================================

title Clear all scanner databases

echo Scanner databases (CryptoScanBot.db) are about to be deleted under:
echo   %DATA%
echo.
echo This throws away the signals, the positions, the zones and the symbol
echo list of EVERY exchange. None of that can be fetched again.
echo.
echo The candle databases, the settings and the logs are NOT touched.
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

rem A typed word and not choice /C YN: this one is not undoable, so it should
rem not be reachable by leaning on a key.
set "ANSWER="
set /p "ANSWER=Type DELETE to confirm, anything else cancels: "
if /I not "%ANSWER%"=="DELETE" goto :cancelled

echo.

set "FAILED="

call :database Binance Spot
call :database Binance Futures
call :database BitMart Futures
call :database Bitvavo Spot
call :database BloFin Futures
call :database Bybit Spot
call :database Bybit Futures
call :database "Bybit EU" Spot
call :database Coinbase Spot
call :database HyperLiquid Spot
call :database HyperLiquid Futures
call :database Kraken Spot
call :database Kraken Futures
call :database Kucoin Spot
call :database Kucoin Futures
call :database Mexc Spot
call :database Mexc Futures
call :database Okx Spot
call :database Okx Futures

rem  Alpaca, same as in the start files - remove the rem to take it along:
rem call :database Alpaca Spot

rem  The emulator folders (Binance\Emulator, Binance\Futures.Emulator) are left
rem  out on purpose: their database IS the recorded run, and emptying it throws
rem  away the thing the emulator exists for.

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
rem  %1 = exchange folder, %2 = trading type folder. The scanner database has
rem  the same name in every folder, unlike the candle one next to it.
rem --------------------------------------------------------------------------
:database
echo %~1 %~2
set "FOLDER=%DATA%\%~1\%~2"
set "FILE=%FOLDER%\CryptoScanBot.db"

if not exist "%FOLDER%\" (
    echo    no data folder, skipped
    goto :eof
)
if not exist "%FILE%" (
    echo    no scanner database, skipped
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
