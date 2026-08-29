@echo off
setlocal

rem ==========================================================================
rem  The SPOT half of "3 Start all scanners.cmd": every supported spot market
rem  in its own scanner process, one per data folder, and then the memory
rem  sampling.
rem
rem  Ten markets. The perpetual side lives in "3a Start all Perpetual
rem  scanners.cmd" and the two together are the same nineteen that "3" starts
rem  in one go. Use this one when a night is about the spot markets only - it
rem  halves the number of processes, and on an exchange that counts per IP
rem  ADDRESS it also halves what the machine asks of it.
rem
rem  Running 3a and 3b at the same time is the same as running 3, with one
rem  difference: each of them starts a memory sampling and a heap snapshot of
rem  its own. Use "3" if you want them both, not these two side by side.
rem
rem  One exchange per process: GlobalData.ActiveExchange is singular, so a
rem  process serves one exchange. The data folder is what keeps the databases
rem  and the log files apart, which is what makes the morning report possible.
rem
rem  The scanner creates its data folder itself, so a new market only needs a
rem  line here.
rem
rem  Change these lines if your folders are somewhere else:
set "BIN=E:\CryptoScanBot\bin\Release\Bin"
set "DATA=E:\CryptoScanBot\Data"
rem
rem  Seconds between two starts. Twenty scanners hitting their exchange api
rem  and the disk in the same second only makes the slow ones slower.
set "WAIT=5"
rem ==========================================================================

title Start all Spot scanners

if not exist "%BIN%\CryptoScanBot.exe" (
    echo Not found: %BIN%\CryptoScanBot.exe
    echo Build the scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the spot scanners, %WAIT% seconds apart...
echo.

echo Binance Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Binance Spot" -f "%DATA%\Binance\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bitvavo Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Bitvavo Spot" -f "%DATA%\Bitvavo\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Bybit Spot" -f "%DATA%\Bybit\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit EU Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Bybit EU Spot" -f "%DATA%\Bybit EU\Spot"
timeout /t %WAIT% /nobreak >nul

echo Coinbase Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Coinbase Spot" -f "%DATA%\Coinbase\Spot"
timeout /t %WAIT% /nobreak >nul

echo HyperLiquid Spot
start "" "%BIN%\CryptoScanBot.exe" -e "HyperLiquid Spot" -f "%DATA%\HyperLiquid\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kraken Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Kraken Spot" -f "%DATA%\Kraken\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Kucoin Spot" -f "%DATA%\Kucoin\Spot"
timeout /t %WAIT% /nobreak >nul

echo Mexc Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Mexc Spot" -f "%DATA%\Mexc\Spot"
timeout /t %WAIT% /nobreak >nul

echo Okx Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Okx Spot" -f "%DATA%\Okx\Spot"
timeout /t %WAIT% /nobreak >nul

rem  Alpaca is supported as well, but it needs an api key of its own ("error
rem  unauthorized" without one) and it trades stocks on exchange hours, so it
rem  is not part of a crypto night. Remove the rem to take it along:
rem echo Alpaca
rem start "" "%BIN%\CryptoScanBot.exe" -e "Alpaca" -f "%DATA%\Alpaca"
rem timeout /t %WAIT% /nobreak >nul

rem  Not here: BitMart Spot and BloFin Spot both stand at IsSupported = false in
rem  CryptoDatabase.CreateExchangeList, each with the reason next to it.

echo.
echo All spot scanners have been started.
echo.

rem  First heap snapshot of the exchange under investigation, in its OWN window
rem  so the memory sampling below does not have to wait for the warm-up. The
rem  stop script takes the second one and prints what grew in between. Set
rem  HEAP_EXCHANGE to empty to switch this off.
rem
rem  One exchange, not all of them: a heap dump is about the working set of the
rem  process and these run to 1.6 GB, so twenty exchanges times two snapshots
rem  would be sixty gigabyte of disk for a question about one of them.
rem
rem  Bybit Spot and not the Kucoin Perpetual that "3 Start all scanners.cmd"
rem  names, because that market is not started here. Of the spot markets Bybit
rem  Spot was the one still climbing after the warm-up in the night of
rem  20-08-2026 (+6,8 MB per hour), second only to Kucoin Perpetual over all
rem  markets, so it is the spot market worth the two snapshots.
set "HEAP_EXCHANGE=Bybit Spot"
if not "%HEAP_EXCHANGE%"=="" (
    start "Heap snapshot" powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0heap-diff.ps1" -Mode Snapshot -Exchange "%HEAP_EXCHANGE%"
)

rem  The memory sampling runs in THIS window from here on: it keeps sampling
rem  until the window is closed. Leave it open for the whole run - without it
rem  the memory section of the report stays empty, and a leak only shows as a
rem  slope over many hours.
call "%~dp01 Start memory sampling.cmd"
