@echo off
setlocal

set "REPO_ROOT=%~dp0"
if "%REPO_ROOT:~-1%"=="\" set "REPO_ROOT=%REPO_ROOT:~0,-1%"
set "DOTNET_ROOT=%REPO_ROOT%\.dotnet"
set "DOTNET_EXE=%DOTNET_ROOT%\dotnet.exe"

if not exist "%DOTNET_EXE%" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%REPO_ROOT%\eng\install-dotnet.ps1" -InstallDir "%DOTNET_ROOT%"
    if errorlevel 1 exit /b %errorlevel%
)

set "DOTNET_ROOT=%DOTNET_ROOT%"
set "DOTNET_MULTILEVEL_LOOKUP=0"
"%DOTNET_EXE%" %*
set "EXIT_CODE=%ERRORLEVEL%"
endlocal & exit /b %EXIT_CODE%
