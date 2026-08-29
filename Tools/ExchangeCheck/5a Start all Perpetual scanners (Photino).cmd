@echo off
setlocal

rem ==========================================================================
rem  The PERPETUAL half of "5 Start all scanners (Photino).cmd": every
rem  supported perpetual market in its own scanner process, one per data
rem  folder, and then the memory sampling. This is the Photino build
rem  (CryptoScanBot.Photino.exe), which is a different process name from the
rem  Avalonia one, so the two builds do not see each other.
rem
rem  Nine markets. The spot side lives in "5b Start all Spot scanners
rem  (Photino).cmd" and the two together are the same nineteen that "5" starts
rem  in one go. Use this one when a night is about the perpetual markets only -
rem  it halves the number of processes, and on an exchange that counts per IP
rem  ADDRESS it also halves what the machine asks of it.
rem
rem  Running 5a and 5b at the same time is the same as running 5, with one
rem  difference: each of them starts a memory sampling of its own. Use "5" if
rem  you want one, not these two side by side.
rem
rem  NOTE: one data folder holds one scanner. Starting these while the Avalonia
rem  scanners are up on the same folders fails on the data folder lock - which
rem  is exactly what that lock is for. Stop those first, or point DATA at a set
rem  of folders of its own.
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

title Start all Perpetual scanners (Photino)

if not exist "%BIN%\CryptoScanBot.Photino.exe" (
    echo Not found: %BIN%\CryptoScanBot.Photino.exe
    echo Build the Photino scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the perpetual scanners, %WAIT% seconds apart...
echo.

echo Binance Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Binance Perpetual" -f "%DATA%\Binance\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo BitMart Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BitMart Perpetual" -f "%DATA%\BitMart\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo BloFin Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "BloFin Perpetual" -f "%DATA%\BloFin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Bybit Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Perpetual" -f "%DATA%\Bybit\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo HyperLiquid Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "HyperLiquid Perpetual" -f "%DATA%\HyperLiquid\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kraken Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Perpetual" -f "%DATA%\Kraken\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Perpetual" -f "%DATA%\Kucoin\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Mexc Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Perpetual" -f "%DATA%\Mexc\Perpetual"
timeout /t %WAIT% /nobreak >nul

echo Okx Perpetual
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Perpetual" -f "%DATA%\Okx\Perpetual"
timeout /t %WAIT% /nobreak >nul

rem  Not here: BitMart Spot, BloFin Spot, Bybit EU Perpetual, Alpaca Perpetual,
rem  Bitvavo Perpetual and Coinbase Perpetual all stand at IsSupported = false in
rem  CryptoDatabase.CreateExchangeList, each with the reason next to it. Alpaca
rem  itself is a spot market and stands in "5b Start all Spot scanners
rem  (Photino).cmd".

echo.
echo All perpetual scanners have been started.
echo.

rem  No heap snapshot here, the same choice "5 Start all scanners
rem  (Photino).cmd" makes: heap-diff.ps1 looks for a running CryptoScanBot.exe
rem  by command line and does not know the Photino process name, so it would
rem  find nothing. The memory sampling below does cover both builds.

rem  The memory sampling runs in THIS window from here on: it keeps sampling
rem  until the window is closed. It picks up both builds by itself. Leave it
rem  open for the whole run - without it the memory section of the report stays
rem  empty, and a leak only shows as a slope over many hours.
call "%~dp01 Start memory sampling.cmd"
