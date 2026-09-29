@echo off
rem =================================================================================================
rem  Builds the DEBUG packages for CryptoScanBot.
rem
rem  This is publish-release.cmd with -c Debug instead of -c Release. Everything that is explained in
rem  the header of publish-release.cmd (one shared folder, the Web-then-Photino merge order via a
rem  temp folder plus xcopy /y, tar.exe for the zips, no trimming) applies here unchanged - read that
rem  header before changing the order of any step below.
rem
rem  What differs from the release build:
rem  - Everything is compiled with -c Debug, so code behind #if DEBUG (for example the debug-only
rem    indicators) is included and the JIT does not optimise, which gives exact line numbers and
rem    readable locals in a debugger or a stack trace.
rem  - The emulator follows along: BundleEmulatorIntoPublish passes $(Configuration) on.
rem  - Output goes to a Debug subfolder of the release output folder and every package name ends in
rem    "-debug", so a debug zip can never be mistaken for a release zip or be picked up by the
rem    "dir *.zip" listing at the end of publish-release.cmd.
rem  - Debug and Release use separate obj\ and bin\ folders, so this script does not disturb a
rem    release build. Do still not run it at the same time as publish-release.cmd: both touch the
rem    same project folders and the second one can fail on a locked file.
rem
rem  Three packages, each a folder plus a zip under %PUBLISHDIR% (see the set below) :
rem
rem    CryptoScanBot-<version>-win-x64-debug    scanner + emulator + Photino + web, Windows
rem    CryptoScanBot-<version>-osx-arm64-debug  scanner + emulator + Photino + web, Apple Silicon
rem    CryptoScanBot-<version>-linux-x64-debug  scanner + emulator + Photino + web, Linux (glibc, x64)
rem
rem  Every command below is a plain one-liner: copy any single line into a command prompt to rerun
rem  or test that step on its own (run the "set VERSION" and "set PUBLISHDIR" lines first, they
rem  are used everywhere).
rem =================================================================================================

setlocal
cd /d "%~dp0"

rem The version is read from Directory.Build.props, see publish-release.cmd for the details.
set VERSION=
for /f "tokens=2 delims=<> " %%v in ('findstr /c:"<Version>" Directory.Build.props') do set VERSION=%%v
if not defined VERSION goto noversion

rem Output folder for the packages and their zips. Do NOT end it with a backslash (tar.exe chokes
rem on it, see publish-release.cmd); the next line strips one if it is there anyway.
set PUBLISHDIR=E:\CryptoScanBot\bin\Debug\Build
if "%PUBLISHDIR:~-1%"=="\" set PUBLISHDIR=%PUBLISHDIR:~0,-1%

if not exist "%PUBLISHDIR%" mkdir "%PUBLISHDIR%"

echo.
echo ==================================================================
echo  CryptoScanBot DEBUG %VERSION%
echo ==================================================================


echo.
echo --- 1/3  CryptoScanBot %VERSION% win-x64 DEBUG (scanner + emulator + Photino + web) ---
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug" rmdir /s /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug"
if exist "%PUBLISHDIR%\web-tmp-win-x64" rmdir /s /q "%PUBLISHDIR%\web-tmp-win-x64"
if exist "%PUBLISHDIR%\photino-tmp-win-x64" rmdir /s /q "%PUBLISHDIR%\photino-tmp-win-x64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Debug -r win-x64 --self-contained true -o "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug" --nologo -v minimal
if errorlevel 1 goto failed
dotnet publish CryptoScanner.Web\CryptoScanner.Web.csproj -c Debug -r win-x64 --self-contained true -o "%PUBLISHDIR%\web-tmp-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\web-tmp-win-x64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\web-tmp-win-x64"
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Debug -r win-x64 --self-contained true -o "%PUBLISHDIR%\photino-tmp-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\photino-tmp-win-x64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\photino-tmp-win-x64"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug\libSkiaSharp.pdb" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug\libSkiaSharp.pdb"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug.zip" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64-debug.zip" -C "%PUBLISHDIR%" "CryptoScanBot-%VERSION%-win-x64-debug"
if errorlevel 1 goto failed


echo.
echo --- 2/3  CryptoScanBot %VERSION% osx-arm64 DEBUG (scanner + emulator + Photino + web) ---
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug" rmdir /s /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug"
if exist "%PUBLISHDIR%\web-tmp-osx-arm64" rmdir /s /q "%PUBLISHDIR%\web-tmp-osx-arm64"
if exist "%PUBLISHDIR%\photino-tmp-osx-arm64" rmdir /s /q "%PUBLISHDIR%\photino-tmp-osx-arm64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Debug -r osx-arm64 --self-contained true -o "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug" --nologo -v minimal
if errorlevel 1 goto failed
dotnet publish CryptoScanner.Web\CryptoScanner.Web.csproj -c Debug -r osx-arm64 --self-contained true -o "%PUBLISHDIR%\web-tmp-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\web-tmp-osx-arm64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\web-tmp-osx-arm64"
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Debug -r osx-arm64 --self-contained true -o "%PUBLISHDIR%\photino-tmp-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\photino-tmp-osx-arm64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\photino-tmp-osx-arm64"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug\libSkiaSharp.pdb" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug\libSkiaSharp.pdb"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug.zip" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64-debug.zip" -C "%PUBLISHDIR%" "CryptoScanBot-%VERSION%-osx-arm64-debug"
if errorlevel 1 goto failed


echo.
echo --- 3/3  CryptoScanBot %VERSION% linux-x64 DEBUG (scanner + emulator + Photino + web) ---
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug" rmdir /s /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug"
if exist "%PUBLISHDIR%\web-tmp-linux-x64" rmdir /s /q "%PUBLISHDIR%\web-tmp-linux-x64"
if exist "%PUBLISHDIR%\photino-tmp-linux-x64" rmdir /s /q "%PUBLISHDIR%\photino-tmp-linux-x64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Debug -r linux-x64 --self-contained true -o "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug" --nologo -v minimal
if errorlevel 1 goto failed
dotnet publish CryptoScanner.Web\CryptoScanner.Web.csproj -c Debug -r linux-x64 --self-contained true -o "%PUBLISHDIR%\web-tmp-linux-x64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\web-tmp-linux-x64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\web-tmp-linux-x64"
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Debug -r linux-x64 --self-contained true -o "%PUBLISHDIR%\photino-tmp-linux-x64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\photino-tmp-linux-x64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\photino-tmp-linux-x64"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug\libSkiaSharp.pdb" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug\libSkiaSharp.pdb"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug.zip" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "%PUBLISHDIR%\CryptoScanBot-%VERSION%-linux-x64-debug.zip" -C "%PUBLISHDIR%" "CryptoScanBot-%VERSION%-linux-x64-debug"
if errorlevel 1 goto failed


echo.
echo ==================================================================
echo  Debug %VERSION% ready - NOT for the GitHub release:
echo ==================================================================
dir /b "%PUBLISHDIR%\*-%VERSION%-*-debug.zip"
echo.
echo After unpacking on macOS or Linux, the same chmod / xattr / apt-get
echo steps apply as for the release build (see publish-release.cmd).
echo.
endlocal
exit /b 0

:failed
echo.
echo *** BUILD FAILED (errorlevel %errorlevel%) - see the output above ***
endlocal
exit /b 1

:noversion
echo.
echo *** Could not read ^<Version^> from Directory.Build.props ***
endlocal
exit /b 1
