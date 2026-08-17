@echo off
setlocal

rem ==========================================================================
rem  Run this in the morning, after stopping the scanners.
rem  It checks every scanner data folder it can find and writes one report per
rem  exchange, then opens the folder holding them.
rem
rem  The reports are html: they open with a double click and carry the colours
rem  and the table of contents. Add --format md to the line below for the plain
rem  text version instead.
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

rem Always open the folder. It used to open only on exit code 0, which meant the
rem folder stayed shut on exactly the mornings something needed looking at.
if exist "%REPORT_FOLDER%" start "" "%REPORT_FOLDER%"

echo.
pause
