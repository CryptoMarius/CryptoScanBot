@echo off
setlocal

rem ==========================================================================
rem  The PERPETUAL half of "3 Start all scanners.cmd": every supported
rem  perpetual market in its own scanner process, one per data folder, and
rem  then the memory sampling.
rem
rem  Nine markets. The spot side lives in "3b Start all Spot scanners.cmd" and
rem  the two together are the same nineteen that "3" starts in one go. Use
rem  this one when a night is about the perpetual markets only - it halves the
rem  number of processes, and on an exchange that counts per IP ADDRESS it
rem  also halves what the machine asks of it.
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

title Start all Perpetual scanners

if not exist "%BIN%\CryptoScanBot.exe" (
    echo Not found: %BIN%\CryptoScanBot.exe
    echo Build the scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the perpetual scanners, %WAIT% seconds apart...
echo.

echo Binance Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Binance Perpetual" -f "%DATA%\Binance\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo BitMart Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "BitMart Perpetual" -f "%DATA%\BitMart\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo BloFin Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "BloFin Perpetual" -f "%DATA%\BloFin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Bybit Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Bybit Perpetual" -f "%DATA%\Bybit\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo HyperLiquid Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "HyperLiquid Perpetual" -f "%DATA%\HyperLiquid\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kraken Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Kraken Perpetual" -f "%DATA%\Kraken\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Kucoin Perpetual" -f "%DATA%\Kucoin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Mexc Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Mexc Perpetual" -f "%DATA%\Mexc\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Okx Perpetual
start "" "%BIN%\CryptoScanBot.exe" -e "Okx Perpetual" -f "%DATA%\Okx\Perpetual"
timeout /t %WAIT% /nobreak >nul

rem  Not here: BitMart Spot, BloFin Spot, Bybit EU Perpetual, Alpaca Perpetual,
rem  Bitvavo Perpetual and Coinbase Perpetual all stand at IsSupported = false in
rem  CryptoDatabase.CreateExchangeList, each with the reason next to it. Alpaca
rem  itself is a spot market and stands in "3b Start all Spot scanners.cmd".

echo.
echo All perpetual scanners have been started.
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
rem  Kucoin Perpetual, the same choice "3 Start all scanners.cmd" makes and for
rem  the same reason: measured over the night of 20-08-2026 it was the market
rem  that was still climbing hardest after the warm-up (+9,1 MB per hour). The
rem  market was called Kucoin Futures then; it was renamed on 27-08-2026
rem  (commit d695b574) and the scanner matches this setting against the name it
rem  has now.
set "HEAP_EXCHANGE=Kucoin Perpetual"
if not "%HEAP_EXCHANGE%"=="" (
    start "Heap snapshot" powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0heap-diff.ps1" -Mode Snapshot -Exchange "%HEAP_EXCHANGE%"
)

rem  The memory sampling runs in THIS window from here on: it keeps sampling
rem  until the window is closed. Leave it open for the whole run - without it
rem  the memory section of the report stays empty, and a leak only shows as a
rem  slope over many hours.
call "%~dp01 Start memory sampling.cmd"
