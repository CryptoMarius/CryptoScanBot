@echo off
setlocal

rem ==========================================================================
rem  Same as "3 Start all scanners.cmd", but for the Photino build
rem  (CryptoScanBot.Photino.exe). That is a different process name, so the two
rem  builds do not see each other.
rem
rem  NOTE: one data folder holds one scanner. Starting these while the Avalonia
rem  scanners are up on the same folders fails on the data folder lock - which
rem  is exactly what that lock is for. Stop those first, or point DATA at a set
rem  of folders of its own.
rem
rem  Change these lines if your folders are somewhere else:
set "BIN=E:\CryptoScanBot\bin\Release\Bin"
set "DATA=E:\CryptoScanBot\Data"
set "WAIT=5"
rem ==========================================================================

title Start all scanners (Photino)

if not exist "%BIN%\CryptoScanBot.Photino.exe" (
    echo Not found: %BIN%\CryptoScanBot.Photino.exe
    echo Build the Photino scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the scanners, %WAIT% seconds apart...
echo.

echo Binance Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Binance Spot" -f "%DATA%\Binance\Spot"
timeout /t %WAIT% /nobreak >nul

echo Binance Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Binance Futures" -f "%DATA%\Binance\Futures"
timeout /t %WAIT% /nobreak >nul

echo BitMart Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BitMart Futures" -f "%DATA%\BitMart\Futures"
timeout /t %WAIT% /nobreak >nul

echo Bitvavo Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bitvavo Spot" -f "%DATA%\Bitvavo\Spot"
timeout /t %WAIT% /nobreak >nul

echo BloFin Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BloFin Futures" -f "%DATA%\BloFin\Futures"
timeout /t %WAIT% /nobreak >nul

echo Bybit Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Spot" -f "%DATA%\Bybit\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Futures" -f "%DATA%\Bybit\Futures"
timeout /t %WAIT% /nobreak >nul

echo Bybit EU Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit EU Spot" -f "%DATA%\Bybit EU\Spot"
timeout /t %WAIT% /nobreak >nul

echo Coinbase Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Coinbase Spot" -f "%DATA%\Coinbase\Spot"
timeout /t %WAIT% /nobreak >nul

echo HyperLiquid Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "HyperLiquid Spot" -f "%DATA%\HyperLiquid\Spot"
timeout /t %WAIT% /nobreak >nul

echo HyperLiquid Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "HyperLiquid Futures" -f "%DATA%\HyperLiquid\Futures"
timeout /t %WAIT% /nobreak >nul

echo Kraken Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Spot" -f "%DATA%\Kraken\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kraken Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Futures" -f "%DATA%\Kraken\Futures"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Spot" -f "%DATA%\Kucoin\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Futures" -f "%DATA%\Kucoin\Futures"
timeout /t %WAIT% /nobreak >nul

echo Mexc Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Spot" -f "%DATA%\Mexc\Spot"
timeout /t %WAIT% /nobreak >nul

echo Mexc Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Futures" -f "%DATA%\Mexc\Futures"
timeout /t %WAIT% /nobreak >nul

echo Okx Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Spot" -f "%DATA%\Okx\Spot"
timeout /t %WAIT% /nobreak >nul

echo Okx Futures
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Futures" -f "%DATA%\Okx\Futures"
timeout /t %WAIT% /nobreak >nul

echo.
echo All scanners have been started.
echo.

rem  The memory sampling runs in THIS window from here on and picks up both
rem  builds by itself. Leave it open for the whole run.
call "%~dp01 Start memory sampling.cmd"
