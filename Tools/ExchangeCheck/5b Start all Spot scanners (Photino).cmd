@echo off
setlocal

rem ==========================================================================
rem  The SPOT half of "5 Start all scanners (Photino).cmd": every supported
rem  spot market in its own scanner process, one per data folder, and then the
rem  memory sampling. This is the Photino build (CryptoScanBot.Photino.exe),
rem  which is a different process name from the Avalonia one, so the two builds
rem  do not see each other.
rem
rem  Ten markets. The perpetual side lives in "5a Start all Perpetual scanners
rem  (Photino).cmd" and the two together are the same nineteen that "5" starts
rem  in one go. Use this one when a night is about the spot markets only - it
rem  halves the number of processes, and on an exchange that counts per IP
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

title Start all Spot scanners (Photino)

if not exist "%BIN%\CryptoScanBot.Photino.exe" (
    echo Not found: %BIN%\CryptoScanBot.Photino.exe
    echo Build the Photino scanner first, or correct the BIN line above.
    echo.
    pause
    exit /b 1
)

echo Starting the spot scanners, %WAIT% seconds apart...
echo.

echo Binance Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Binance Spot" -f "%DATA%\Binance\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bitvavo Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bitvavo Spot" -f "%DATA%\Bitvavo\Spot"
timeout /t %WAIT% /nobreak >nul

echo Bybit Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Bybit Spot" -f "%DATA%\Bybit\Spot"
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

echo Kraken Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kraken Spot" -f "%DATA%\Kraken\Spot"
timeout /t %WAIT% /nobreak >nul

echo Kucoin Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Kucoin Spot" -f "%DATA%\Kucoin\Spot"
timeout /t %WAIT% /nobreak >nul

echo Mexc Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Mexc Spot" -f "%DATA%\Mexc\Spot"
timeout /t %WAIT% /nobreak >nul

echo Okx Spot
start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Okx Spot" -f "%DATA%\Okx\Spot"
timeout /t %WAIT% /nobreak >nul

rem  Alpaca is supported as well, but it needs an api key of its own ("error
rem  unauthorized" without one) and it trades stocks on exchange hours, so it
rem  is not part of a crypto night. Remove the rem to take it along:
rem echo Alpaca
rem start "" "%BIN%\CryptoScanBot.Photino.exe" -e "Alpaca" -f "%DATA%\Alpaca"
rem timeout /t %WAIT% /nobreak >nul

rem  Not here: BitMart Spot and BloFin Spot both stand at IsSupported = false in
rem  CryptoDatabase.CreateExchangeList, each with the reason next to it.

echo.
echo All spot scanners have been started.
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
