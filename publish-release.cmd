@echo off
rem =================================================================================================
rem  Builds the release packages for CryptoScanBot.
rem
rem  Four packages, each a folder plus a zip under publish\ :
rem
rem    CryptoScanBot-<version>-win-x64            scanner + emulator, Windows
rem    CryptoScanBot-<version>-osx-arm64          scanner + emulator, Apple Silicon
rem    CryptoScanBot.Photino-<version>-win-x64    Photino application, Windows
rem    CryptoScanBot.Photino-<version>-osx-arm64  Photino application, Apple Silicon
rem
rem  The scanner csproj has a BundleEmulatorIntoPublish target, so CryptoScanBot.Emulator lands in
rem  the same folder as the scanner - it does not need its own publish command here.
rem
rem  Every command below is a plain one-liner: copy any single line into a command prompt to rerun
rem  or test that step on its own (run the "set VERSION" line first, it is used everywhere).
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
rem    runtime identifier in all four lines.
rem =================================================================================================

setlocal
cd /d "%~dp0"

rem Keep this in sync with <Version> in Directory.Build.props.
set VERSION=2.6.0

if not exist "publish" mkdir "publish"

echo.
echo ==================================================================
echo  CryptoScanBot release %VERSION%
echo ==================================================================


echo.
echo --- 1/4  CryptoScanBot %VERSION% win-x64 (scanner + emulator) ---
if exist "publish\CryptoScanBot-%VERSION%-win-x64" rmdir /s /q "publish\CryptoScanBot-%VERSION%-win-x64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Release -r win-x64 --self-contained true -o "publish\CryptoScanBot-%VERSION%-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
if exist "publish\CryptoScanBot-%VERSION%-win-x64\libSkiaSharp.pdb" del /q "publish\CryptoScanBot-%VERSION%-win-x64\libSkiaSharp.pdb"
if exist "publish\CryptoScanBot-%VERSION%-win-x64.zip" del /q "publish\CryptoScanBot-%VERSION%-win-x64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "publish\CryptoScanBot-%VERSION%-win-x64.zip" -C "publish" "CryptoScanBot-%VERSION%-win-x64"
if errorlevel 1 goto failed


echo.
echo --- 2/4  CryptoScanBot %VERSION% osx-arm64 (scanner + emulator) ---
if exist "publish\CryptoScanBot-%VERSION%-osx-arm64" rmdir /s /q "publish\CryptoScanBot-%VERSION%-osx-arm64"
dotnet publish CryptoScanner\CryptoScanner.csproj -c Release -r osx-arm64 --self-contained true -o "publish\CryptoScanBot-%VERSION%-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
if exist "publish\CryptoScanBot-%VERSION%-osx-arm64\libSkiaSharp.pdb" del /q "publish\CryptoScanBot-%VERSION%-osx-arm64\libSkiaSharp.pdb"
if exist "publish\CryptoScanBot-%VERSION%-osx-arm64.zip" del /q "publish\CryptoScanBot-%VERSION%-osx-arm64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "publish\CryptoScanBot-%VERSION%-osx-arm64.zip" -C "publish" "CryptoScanBot-%VERSION%-osx-arm64"
if errorlevel 1 goto failed


echo.
echo --- 3/4  CryptoScanBot.Photino %VERSION% win-x64 ---
if exist "publish\CryptoScanBot.Photino-%VERSION%-win-x64" rmdir /s /q "publish\CryptoScanBot.Photino-%VERSION%-win-x64"
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Release -r win-x64 --self-contained true -o "publish\CryptoScanBot.Photino-%VERSION%-win-x64" --nologo -v minimal
if errorlevel 1 goto failed
if exist "publish\CryptoScanBot.Photino-%VERSION%-win-x64\libSkiaSharp.pdb" del /q "publish\CryptoScanBot.Photino-%VERSION%-win-x64\libSkiaSharp.pdb"
if exist "publish\CryptoScanBot.Photino-%VERSION%-win-x64.zip" del /q "publish\CryptoScanBot.Photino-%VERSION%-win-x64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "publish\CryptoScanBot.Photino-%VERSION%-win-x64.zip" -C "publish" "CryptoScanBot.Photino-%VERSION%-win-x64"
if errorlevel 1 goto failed


echo.
echo --- 4/4  CryptoScanBot.Photino %VERSION% osx-arm64 ---
if exist "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64" rmdir /s /q "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64"
dotnet publish CryptoScanner.Photino\CryptoScanner.Photino.csproj -c Release -r osx-arm64 --self-contained true -o "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64" --nologo -v minimal
if errorlevel 1 goto failed
if exist "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64\libSkiaSharp.pdb" del /q "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64\libSkiaSharp.pdb"
if exist "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64.zip" del /q "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64.zip"
"%SystemRoot%\System32\tar.exe" -a -c -f "publish\CryptoScanBot.Photino-%VERSION%-osx-arm64.zip" -C "publish" "CryptoScanBot.Photino-%VERSION%-osx-arm64"
if errorlevel 1 goto failed


echo.
echo ==================================================================
echo  Release %VERSION% ready - attach these to the GitHub release:
echo ==================================================================
dir /b "publish\*-%VERSION%-*.zip"
echo.
echo The macOS packages are unsigned and were zipped on Windows, so the
echo executable bit is gone. After unpacking, on the Mac:
echo     chmod +x CryptoScanBot CryptoScanBot.Emulator
echo     xattr -dr com.apple.quarantine .
echo.
endlocal
exit /b 0

:failed
echo.
echo *** BUILD FAILED (errorlevel %errorlevel%) - see the output above ***
endlocal
exit /b 1
