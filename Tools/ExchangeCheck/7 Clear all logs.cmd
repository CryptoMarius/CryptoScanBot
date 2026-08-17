@echo off
setlocal

rem ==========================================================================
rem  Empties the Log folder of every supported exchange, so the next run
rem  starts on a clean slate and the report cannot pick up yesterday's errors.
rem
rem  ONLY the log files go. The databases, the settings and the candles are
rem  left alone - this is a reset of what is written ABOUT the run, not of the
rem  run's data.
rem
rem  Stop the scanners first (4 Stop all scanners.cmd). A running scanner holds
rem  its log file open, so those files stay behind and you end up with half a
rem  reset. This file checks and warns.
rem
rem  Change this line if your folders are somewhere else:
set "DATA=E:\CryptoScanBot\Data"
rem ==========================================================================

title Clear all logs

echo Log files are about to be deleted under:
echo   %DATA%
echo.
echo Databases, settings and candles are NOT touched.
echo.

rem Warn about anything that is still running - those log files are locked and
rem will survive the delete, which is worse than not deleting at all because it
rem looks like it worked.
rem findstr and not find: on a machine with git for windows on the path, "find" is the unix one and
rem it does not understand this at all.
set "RUNNING="
tasklist /FI "IMAGENAME eq CryptoScanBot.exe" /NH 2>nul | findstr /I "CryptoScanBot" >nul && set "RUNNING=1"
tasklist /FI "IMAGENAME eq CryptoScanBot.Photino.exe" /NH 2>nul | findstr /I "CryptoScanBot" >nul && set "RUNNING=1"

if defined RUNNING (
    echo *** WARNING: scanners are still running. ***
    echo Their log files are open and will NOT be deleted. Stop them first.
    echo.
)

choice /C YN /N /M "Delete the log files? [Y/N] "
if errorlevel 2 goto :cancelled

echo.

echo Binance Spot
del /f /q "%DATA%\Binance\Spot\Log\*.*" 2>nul

echo Binance Futures
del /f /q "%DATA%\Binance\Futures\Log\*.*" 2>nul

echo BitMart Futures
del /f /q "%DATA%\BitMart\Futures\Log\*.*" 2>nul

echo Bitvavo Spot
del /f /q "%DATA%\Bitvavo\Spot\Log\*.*" 2>nul

echo BloFin Futures
del /f /q "%DATA%\BloFin\Futures\Log\*.*" 2>nul

echo Bybit Spot
del /f /q "%DATA%\Bybit\Spot\Log\*.*" 2>nul

echo Bybit Futures
del /f /q "%DATA%\Bybit\Futures\Log\*.*" 2>nul

echo Bybit EU Spot
del /f /q "%DATA%\Bybit EU\Spot\Log\*.*" 2>nul

echo Coinbase Spot
del /f /q "%DATA%\Coinbase\Spot\Log\*.*" 2>nul

echo HyperLiquid Spot
del /f /q "%DATA%\HyperLiquid\Spot\Log\*.*" 2>nul

echo HyperLiquid Futures
del /f /q "%DATA%\HyperLiquid\Futures\Log\*.*" 2>nul

echo Kraken Spot
del /f /q "%DATA%\Kraken\Spot\Log\*.*" 2>nul

echo Kraken Futures
del /f /q "%DATA%\Kraken\Futures\Log\*.*" 2>nul

echo Kucoin Spot
del /f /q "%DATA%\Kucoin\Spot\Log\*.*" 2>nul

echo Kucoin Futures
del /f /q "%DATA%\Kucoin\Futures\Log\*.*" 2>nul

echo Mexc Spot
del /f /q "%DATA%\Mexc\Spot\Log\*.*" 2>nul

echo Mexc Futures
del /f /q "%DATA%\Mexc\Futures\Log\*.*" 2>nul

echo Okx Spot
del /f /q "%DATA%\Okx\Spot\Log\*.*" 2>nul

echo Okx Futures
del /f /q "%DATA%\Okx\Futures\Log\*.*" 2>nul

rem  Alpaca, same as in the start files - remove the rem to take it along:
rem echo Alpaca
rem del /f /q "%DATA%\Alpaca\Log\*.*" 2>nul

echo.
pause
exit /b 0

:cancelled
echo.
echo Cancelled, nothing was deleted.
echo.
pause
exit /b 1
