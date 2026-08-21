@echo off
setlocal

rem ==========================================================================
rem  Starts every supported exchange in its own scanner process, one per data
rem  folder, and then starts the memory sampling.
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

title Start all scanners

if not exist "%BIN%\CryptoScanBot.exe" (
    echo Not found: %BIN%\CryptoScanBot.exe
    echo Build the scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the scanners, %WAIT% seconds apart...
echo.

echo Binance Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Binance Spot" -f "%DATA%\Binance\Spot"
timeout /t %WAIT% /nobreak >nul

echo Binance Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Binance Futures" -f "%DATA%\Binance\Futures"
timeout /t %WAIT% /nobreak >nul

echo BitMart Futures
start "" "%BIN%\CryptoScanBot.exe" -e "BitMart Futures" -f "%DATA%\BitMart\Futures"
timeout /t %WAIT% /nobreak >nul

echo Bitvavo Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Bitvavo Spot" -f "%DATA%\Bitvavo\Spot"
timeout /t %WAIT% /nobreak >nul

echo BloFin Futures
start "" "%BIN%\CryptoScanBot.exe" -e "BloFin Futures" -f "%DATA%\BloFin\Futures"
timeout /t %WAIT% /nobreak >nul

echo Bybit Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Bybit Spot" -f "%DATA%\Bybit\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Bybit Futures" -f "%DATA%\Bybit\Futures"
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

echo HyperLiquid Futures
start "" "%BIN%\CryptoScanBot.exe" -e "HyperLiquid Futures" -f "%DATA%\HyperLiquid\Futures"
timeout /t %WAIT% /nobreak >nul

echo Kraken Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Kraken Spot" -f "%DATA%\Kraken\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kraken Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Kraken Futures" -f "%DATA%\Kraken\Futures"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Kucoin Spot" -f "%DATA%\Kucoin\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Kucoin Futures" -f "%DATA%\Kucoin\Futures"
timeout /t %WAIT% /nobreak >nul

echo Mexc Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Mexc Spot" -f "%DATA%\Mexc\Spot"
timeout /t %WAIT% /nobreak >nul

echo Mexc Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Mexc Futures" -f "%DATA%\Mexc\Futures"
timeout /t %WAIT% /nobreak >nul

echo Okx Spot
start "" "%BIN%\CryptoScanBot.exe" -e "Okx Spot" -f "%DATA%\Okx\Spot"
timeout /t %WAIT% /nobreak >nul

echo Okx Futures
start "" "%BIN%\CryptoScanBot.exe" -e "Okx Futures" -f "%DATA%\Okx\Futures"
timeout /t %WAIT% /nobreak >nul

rem  Alpaca is supported as well, but it needs an api key of its own ("error
rem  unauthorized" without one) and it trades stocks on exchange hours, so it
rem  is not part of a crypto night. Remove the rem to take it along:
rem echo Alpaca
rem start "" "%BIN%\CryptoScanBot.exe" -e "Alpaca" -f "%DATA%\Alpaca"
rem timeout /t %WAIT% /nobreak >nul

echo.
echo All scanners have been started.
echo.

rem  First heap snapshot of the exchange under investigation, in its OWN window
rem  so the memory sampling below does not have to wait for the warm-up. The
rem  stop script takes the second one and prints what grew in between. Set
rem  HEAP_EXCHANGE to empty to switch this off.
rem
rem  One exchange, not all of them: a heap dump is about the working set of the
rem  process and these run to 1.6 GB, so twenty exchanges times two snapshots
rem  would be sixty gigabyte of disk for a question about one of them.
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
    start "Heap snapshot" powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0heap-diff.ps1" -Mode Snapshot -Exchange "%HEAP_EXCHANGE%"
)

rem  The memory sampling runs in THIS window from here on: it keeps sampling
rem  until the window is closed. Leave it open for the whole run - without it
rem  the memory section of the report stays empty, and a leak only shows as a
rem  slope over many hours.
call "%~dp01 Start memory sampling.cmd"
