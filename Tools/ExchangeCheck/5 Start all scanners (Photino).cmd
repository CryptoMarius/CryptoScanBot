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

echo Binance Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Binance Perpetual" -f "%DATA%\Binance\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo BitMart Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BitMart Perpetual" -f "%DATA%\BitMart\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Bitvavo Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bitvavo Spot" -f "%DATA%\Bitvavo\Spot"
timeout /t %WAIT% /nobreak >nul

echo BloFin Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BloFin Perpetual" -f "%DATA%\BloFin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Bybit Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Spot" -f "%DATA%\Bybit\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Perpetual" -f "%DATA%\Bybit\Perpetual"
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

echo HyperLiquid Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "HyperLiquid Perpetual" -f "%DATA%\HyperLiquid\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kraken Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Spot" -f "%DATA%\Kraken\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kraken Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Perpetual" -f "%DATA%\Kraken\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Spot" -f "%DATA%\Kucoin\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Perpetual" -f "%DATA%\Kucoin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Mexc Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Spot" -f "%DATA%\Mexc\Spot"
timeout /t %WAIT% /nobreak >nul

echo Mexc Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Perpetual" -f "%DATA%\Mexc\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Okx Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Spot" -f "%DATA%\Okx\Spot"
timeout /t %WAIT% /nobreak >nul

echo Okx Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Perpetual" -f "%DATA%\Okx\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo.
echo All scanners have been started.
echo.

rem  The memory sampling runs in THIS window from here on and picks up both
rem  builds by itself. Leave it open for the whole run.
call "%~dp01 Start memory sampling.cmd"
