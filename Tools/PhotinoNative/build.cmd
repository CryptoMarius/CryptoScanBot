@echo off
setlocal

rem ==========================================================================
rem  Builds a patched Photino.Native.dll (x64, Release) with the web message
rem  leak fixed, and drops it next to this file. CryptoScanner.Photino.csproj
rem  copies that dll over the one from the Photino.Native package on every
rem  build and publish, as long as it is here.
rem
rem  Needs, once:
rem    - Visual Studio with the "Desktop development with C++" workload
rem      (MSVC toolset + Windows 10/11 SDK). Install with:
rem        "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\setup.exe" modify
rem            --installPath "F:\Microsoft Visual Studio\18\Community"
rem            --add Microsoft.VisualStudio.Workload.NativeDesktop --includeRecommended --passive
rem    - git on the path (the source is cloned into %TEMP%).
rem
rem  Change VS_ROOT if Visual Studio lives somewhere else.
rem ==========================================================================
set "VS_ROOT=F:\Microsoft Visual Studio\18\Community"
set "SRC=%TEMP%\photino.Native"
set "HERE=%~dp0"

call "%VS_ROOT%\Common7\Tools\VsDevCmd.bat" -arch=amd64 -host_arch=amd64 >nul
if errorlevel 1 (
    echo Could not find VsDevCmd.bat under "%VS_ROOT%". Is the C++ workload installed?
    exit /b 1
)

if not exist "%SRC%\.git" (
    rem The Windows certificate store, because the AVG scanner on this machine re-signs https.
    git -c http.sslBackend=schannel clone https://github.com/tryphotino/photino.Native.git "%SRC%" || exit /b 1
)

pushd "%SRC%"
git checkout -q -- .
git apply --whitespace=nowarn "%HERE%photino-native-webmessage-leak.patch" || (echo Patch did not apply - upstream changed? & popd & exit /b 1)

rem packages.config restore (WebView2 SDK and WIL). MSBuild downloads them into the global
rem packages folder, but the project looks for them in ..\packages next to the solution in the
rem old Id.Version layout, so copy them over from wherever the global folder is.
msbuild Photino.Native\Photino.Native.vcxproj -t:Restore -p:RestorePackagesConfig=true -p:RestorePackagesPath="%SRC%\packages" -p:Configuration=Release -p:Platform=x64 -v:m || (popd & exit /b 1)
if not exist packages mkdir packages
rem On 03-09-2026 the restore reported "Adding package ... to folder 'E:\Visual Studio\NuGetPackages'"
rem (the repositoryPath of the machine-wide NuGet.config) and left ..\packages empty, so look in the
rem usual places and copy what is there.
for /f "tokens=1,* delims= " %%a in ('dotnet nuget locals global-packages -l') do set "GLOBAL=%%b"
call :findpackage Microsoft.Web.WebView2 1.0.2903.40
call :findpackage Microsoft.Windows.ImplementationLibrary 1.0.240803.1
if not exist "packages\Microsoft.Web.WebView2.1.0.2903.40\build\native\Microsoft.Web.WebView2.targets" (
    echo The WebView2 package was restored somewhere this script did not look. Copy the folders
    echo Microsoft.Web.WebView2.1.0.2903.40 and Microsoft.Windows.ImplementationLibrary.1.0.240803.1
    echo into %SRC%\packages and run again.
    popd & exit /b 1
)
goto :build

:findpackage
if exist "packages\%1.%2\build" exit /b 0
for %%r in ("%GLOBAL%" "%USERPROFILE%\.nuget\packages" "E:\Visual Studio\NuGetPackages" "F:\Microsoft Visual Studio\NuGetPackages") do (
    if exist "%%~r\%1.%2\build" xcopy /e /i /q "%%~r\%1.%2" "packages\%1.%2" >nul & if exist "packages\%1.%2\build" exit /b 0
    if exist "%%~r\%1\%2\build" xcopy /e /i /q "%%~r\%1\%2" "packages\%1.%2" >nul & if exist "packages\%1.%2\build" exit /b 0
)
exit /b 0

:build
rem The project asks for toolset v143 (Visual Studio 2022). Visual Studio 18 installs v145 by
rem default and v143 only as an extra component, so try the project's own toolset first and fall
rem back to v145 when it is not there.
msbuild Photino.Native\Photino.Native.vcxproj -p:Configuration=Release -p:Platform=x64 -v:m
if errorlevel 1 (
    echo Toolset v143 not available, retrying with v145 ...
    msbuild Photino.Native\Photino.Native.vcxproj -p:Configuration=Release -p:Platform=x64 -p:PlatformToolset=v145 -v:m || (popd & exit /b 1)
)
popd

copy /y "%SRC%\Photino.Native\x64\Release\Photino.Native.dll" "%HERE%Photino.Native.dll" >nul || exit /b 1
echo.
echo Patched Photino.Native.dll written to %HERE%
echo Rebuild or publish CryptoScanner.Photino to pick it up; a running scanner keeps the old dll until it restarts.
endlocal
