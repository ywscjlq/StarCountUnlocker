@echo off
setlocal
cd /d "%~dp0"
set "DSP_DIR=F:\SteamLibrary\steamapps\common\Dyson Sphere Program"

REM Find csc.exe
set CSC=
for %%d in (
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) do if exist %%d set CSC=%%d

if "%CSC%"=="" (
    echo [ERROR] csc.exe not found! Install .NET Framework 4.8 Developer Pack
    echo https://dotnet.microsoft.com/download/dotnet-framework/net48
    REM pause removed for auto-build & exit /b 1
)

echo Using: %CSC%

set REFS=
set REFS=%REFS% /r:"%DSP_DIR%\DSPGAME_Data\Managed\Assembly-CSharp.dll"
set REFS=%REFS% /r:"%DSP_DIR%\DSPGAME_Data\Managed\UnityEngine.CoreModule.dll"
set REFS=%REFS% /r:"%DSP_DIR%\DSPGAME_Data\Managed\UnityEngine.dll"
set REFS=%REFS% /r:"%DSP_DIR%\DSPGAME_Data\Managed\UnityEngine.UI.dll"
set REFS=%REFS% /r:"%DSP_DIR%\BepInEx\core\BepInEx.dll"
set REFS=%REFS% /r:"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
set REFS=%REFS% /r:"%DSP_DIR%\DSPGAME_Data\Managed\UnityEngine.InputLegacyModule.dll"
set REFS=%REFS% /r:"%DSP_DIR%\BepInEx\core\0Harmony.dll"

%CSC% /target:library /out:StarCountUnlocker.dll %REFS% Plugin.cs

if %errorlevel% neq 0 (
    echo [FAILED] Build error!
    REM pause removed for auto-build & exit /b 1
)

echo [OK] StarCountUnlocker.dll built!

REM Deploy
copy /Y StarCountUnlocker.dll "%DSP_DIR%\BepInEx\plugins\"
echo [OK] Deployed to BepInEx\plugins\
echo.
echo === Done! Restart DSP to activate. ===
REM pause removed for auto-build
