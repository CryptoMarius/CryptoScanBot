@echo off
setlocal

rem ==========================================================================
rem  Run this in the morning, after stopping the scanners.
rem  It checks every scanner data folder it can find and writes one report per
rem  exchange, then opens the folder holding them.
rem
rem  Change these lines if your folders are somewhere else:
set "DATA_FOLDER=E:\CryptoScanBot\Data"
set "SAMPLE_FOLDER=E:\CryptoScanBot\Data\Memory"
set "REPORT_FOLDER=E:\CryptoScanBot\Data\Reports"
rem ==========================================================================

title Exchange reports

where python >nul 2>&1
if errorlevel 1 (
    echo Python was not found on the path.
    echo Install Python 3, or open a command prompt and run check_all.py yourself.
    echo.
    pause
    exit /b 1
)

python "%~dp0check_all.py" --base "%DATA_FOLDER%" --memory "%SAMPLE_FOLDER%" --out "%REPORT_FOLDER%"
set "RESULT=%ERRORLEVEL%"

if "%RESULT%"=="0" (
    if exist "%REPORT_FOLDER%" start "" "%REPORT_FOLDER%"
)

echo.
pause
