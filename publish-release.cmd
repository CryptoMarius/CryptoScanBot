@echo off
rem =================================================================================================
rem  Builds the release packages for CryptoScanBot.
rem
rem  Two packages, each a folder plus a zip under %PUBLISHDIR% (see the set below) :
rem
rem    CryptoScanBot-<version>-win-x64    scanner + emulator + Photino, Windows
rem    CryptoScanBot-<version>-osx-arm64  scanner + emulator + Photino, Apple Silicon
rem
rem  All three applications share one folder. They are built against the same project references, so
rem  483 of the 505 files are byte for byte identical (the whole .NET runtime, the exchange
rem  libraries, Avalonia, Skia); shipping them separately duplicated about 276 MB per runtime.
rem  Every application keeps its own .deps.json / .runtimeconfig.json, so assembly resolution stays
rem  separate per application and there is no clash.
rem
rem  The scanner csproj has a BundleEmulatorIntoPublish target, so CryptoScanBot.Emulator lands in
rem  the same folder as the scanner - it does not need its own publish command here.
rem
rem  ORDER MATTERS on Windows. Exactly two files exist in both packages with different versions, and
rem  the Photino ones are the ones all three .deps.json files ask for:
rem    - System.Collections.Immutable.dll  scanner 8.0.0.0 vs Photino 10.0.0.0 (all deps.json want 10.0.7)
rem    - WebView2Loader.dll (native)       1.0.1829.0 (WebView.Avalonia) vs 1.0.2903.40 (Photino.Native)
rem  Photino therefore has to land on top. Publishing it straight into the scanner folder does NOT do
rem  that: publish copies with PreserveNewest semantics, so it keeps whatever file has the newest
rem  timestamp - and the 10.0.7 assembly sits in the NuGet cache with an older date than the 8.0 one
rem  from the runtime pack, so it was silently skipped. Hence the temp folder plus xcopy /y below,
rem  which overwrites unconditionally and makes the result independent of package timestamps.
rem  The 8.0.0.0 file itself comes from the emulator bundle (the emulator publishes the runtime pack
rem  version while its own deps.json declares 10.0.7); the Photino merge repairs that as a side
rem  effect. On osx-arm64 no file differs at all, but the same order is used there to keep the two
rem  blocks identical.
rem
rem  Every command below is a plain one-liner: copy any single line into a command prompt to rerun
rem  or test that step on its own (run the "set VERSION" and "set PUBLISHDIR" lines first, they
rem  are used everywhere).
rem
rem  Notes:
rem  - Self-contained: the .NET runtime ships inside the package, users install nothing.
rem  - Do NOT add --p:PublishTrimmed: Dapper, Blazor, Avalonia and the indicator registration all
rem    use reflection, trimming breaks them silently.
rem  - Debug symbols of our own assemblies are kept (about 1 MB) so stack traces from a user's log
rem    still carry file names and line numbers. Only libSkiaSharp.pdb is dropped: 80 MB of native
rem    Skia symbols that never help with our own exceptions.
rem  - Zipping uses the tar.exe that ships with Windows (bsdtar). It writes forward slashes in the
rem    zip, which matters for the macOS packages - a zip made by Windows Explorer or PowerShell 5.1
rem    stores backslashes and unpacks into a mess on a Mac.
rem  - For an extra runtime (osx-x64 for Intel Macs, linux-x64) copy one block and replace the
rem    runtime identifier everywhere in it.
rem  - Do not run two publishes at the same time: they share the Core/Config/Chart/Analyzers
rem    projects and the second one fails on a locked .pdb in obj\.
rem =================================================================================================

setlocal
cd /d "%~dp0"

rem Keep this in sync with <Version> in Directory.Build.props.
set VERSION=2.6.0

rem Output folder for both packages and their zips. Absolute path, so it does not matter from
rem where the script is started. Do NOT end it with a backslash - the next line strips one if it
rem is there anyway, because tar.exe would otherwise choke: in -C "%PUBLISHDIR%" a trailing
rem backslash escapes the closing quote, the directory argument swallows the folder name that
rem follows it, and tar reports "no files or directories specified".
set PUBLISHDIR=E:\CryptoScanBot\bin\Build
if "%PUBLISHDIR:~-1%"=="\" set PUBLISHDIR=%PUBLISHDIR:~0,-1%

if not exist "%PUBLISHDIR%" mkdir "%PUBLISHDIR%"

echo.
echo ==================================================================
echo  CryptoScanBot release %VERSION%
echo ==================================================================


echo.
echo --- 1/2  CryptoScanBot %VERSION% win-x64 (scanner + emulator + Photino) ---
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64" rmdir /s /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64"
if exist "%PUBLISHDIR%\photino-tmp-win-x64" rmdir /s /q "%PUBLISHDIR%\photino-tmp-win-x64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Release -r win-x64 --self-contained true -o "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Release -r win-x64 --self-contained true -o "%PUBLISHDIR%\photino-tmp-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\photino-tmp-win-x64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\photino-tmp-win-x64"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64\libSkiaSharp.pdb" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64\libSkiaSharp.pdb"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64.zip" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "%PUBLISHDIR%\CryptoScanBot-%VERSION%-win-x64.zip" -C "%PUBLISHDIR%" "CryptoScanBot-%VERSION%-win-x64"
if errorlevel 1 goto failed


echo.
echo --- 2/2  CryptoScanBot %VERSION% osx-arm64 (scanner + emulator + Photino) ---
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64" rmdir /s /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64"
if exist "%PUBLISHDIR%\photino-tmp-osx-arm64" rmdir /s /q "%PUBLISHDIR%\photino-tmp-osx-arm64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Release -r osx-arm64 --self-contained true -o "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Release -r osx-arm64 --self-contained true -o "%PUBLISHDIR%\photino-tmp-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
xcopy "%PUBLISHDIR%\photino-tmp-osx-arm64\*" "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64\" /e /y /r /q >nul
if errorlevel 1 goto failed
rmdir /s /q "%PUBLISHDIR%\photino-tmp-osx-arm64"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64\libSkiaSharp.pdb" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64\libSkiaSharp.pdb"
if exist "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64.zip" del /q "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "%PUBLISHDIR%\CryptoScanBot-%VERSION%-osx-arm64.zip" -C "%PUBLISHDIR%" "CryptoScanBot-%VERSION%-osx-arm64"
if errorlevel 1 goto failed


echo.
echo ==================================================================
echo  Release %VERSION% ready - attach these to the GitHub release:
echo ==================================================================
dir /b "%PUBLISHDIR%\*-%VERSION%-*.zip"
echo.
echo The macOS package is unsigned and was zipped on Windows, so the
echo executable bit is gone. After unpacking, on the Mac:
echo     chmod +x CryptoScanBot CryptoScanBot.Emulator CryptoScanBot.Photino
echo     xattr -dr com.apple.quarantine .
echo.
endlocal
exit /b 0

:failed
echo.
echo *** BUILD FAILED (errorlevel %errorlevel%) - see the output above ***
endlocal
exit /b 1
