@echo off
setlocal EnableExtensions
rem ===================================================================
rem  Void Converter - Unity player build + zip (Windows / Win64)
rem
rem  Usage   : package_unity.bat [version]
rem            package_unity.bat 0.1.4      -> C:\Builds\VoidConverter-v0.1.4 and ...zip
rem            package_unity.bat            -> uses bundleVersion from ProjectSettings
rem  Version : vMILESTONE.FIXES, e.g. 0.1.4. The same number goes into the
rem            folder name, the zip name, version.txt, the in-game corner
rem            label (Player Settings > Version) and the CHANGELOG / git tag.
rem  Env vars (all optional, set them before running):
rem            VERSION     same as the argument
rem            BUILD_DATE  yyyy-MM-dd written to version.txt (default: today)
rem            BUILD_ROOT  folder that holds the builds (default: C:\Builds)
rem            UNITY, PROJECT  paths for this PC (see below)
rem  Output  : <BUILD_ROOT>\VoidConverter-v<version>\  (game)
rem            <BUILD_ROOT>\VoidConverter-v<version>.zip  (give this one out)
rem            <BUILD_ROOT>\VoidConverter-v<version>.log  (Unity build log)
rem  CLOSE the Unity Editor for this project first, or the build fails.
rem  Scenes  : the scene list in Build Profiles (Build Settings).
rem  Comments are in English on purpose: Korean text in .bat files
rem  can break depending on the file encoding / console code page.
rem ===================================================================

rem ---- 1) Change these lines for your PC -----------------------------
if not defined UNITY      set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
if not defined PROJECT    set "PROJECT=C:\Users\user\Documents\GitHub\VoidConverter"
if not defined BUILD_ROOT set "BUILD_ROOT=C:\Builds"
rem -------------------------------------------------------------------

set "RC=1"
set "PAUSE_AT_END=1"
if "%~1"=="" goto :args_done
set "VERSION=%~1"
set "PAUSE_AT_END="
:args_done
if "%PROJECT:~-1%"=="\" set "PROJECT=%PROJECT:~0,-1%"
if "%BUILD_ROOT:~-1%"=="\" set "BUILD_ROOT=%BUILD_ROOT:~0,-1%"

if not exist "%UNITY%" goto :err_engine
if not exist "%PROJECT%\Assets" goto :err_project
for %%P in ("%PROJECT%") do set "GAME=%%~nxP"

rem ---- version: argument, VERSION env var, or the project setting ----
if defined VERSION goto :have_version
for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "(Select-String -Path '%PROJECT%\ProjectSettings\ProjectSettings.asset' -Pattern '^\s*bundleVersion:\s*(\S+)').Matches[0].Groups[1].Value"`) do set "VERSION=%%V"
:have_version
if not defined VERSION goto :err_version
if /i "%VERSION:~0,1%"=="v" set "VERSION=%VERSION:~1%"

rem ---- build date and commit, for version.txt ------------------------
if defined BUILD_DATE goto :have_date
for /f %%D in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd"') do set "BUILD_DATE=%%D"
:have_date
set "COMMIT=unknown"
for /f %%G in ('git -C "%PROJECT%" rev-parse --short HEAD 2^>nul') do set "COMMIT=%%G"

set "OUT_DIR=%BUILD_ROOT%\%GAME%-v%VERSION%"
set "ZIP=%OUT_DIR%.zip"
set "LOG=%OUT_DIR%.log"

if not exist "%BUILD_ROOT%" mkdir "%BUILD_ROOT%"
if exist "%OUT_DIR%" rmdir /s /q "%OUT_DIR%"
mkdir "%OUT_DIR%"
if not exist "%OUT_DIR%" goto :err_out

echo [1/4] Setting the game version to %VERSION%  (ProjectSettings bundleVersion)
powershell -NoProfile -Command "$p='%PROJECT%\ProjectSettings\ProjectSettings.asset'; $t=[IO.File]::ReadAllText($p); $n=[regex]::Replace($t,'(?m)^(\s*bundleVersion:\s*)[^\r\n]*','${1}%VERSION%'); if($n -ne $t){[IO.File]::WriteAllText($p,$n,(New-Object Text.UTF8Encoding($false)))}"
if errorlevel 1 goto :err_version

echo [2/4] Building %GAME% v%VERSION% [Win64]
echo       Output : %OUT_DIR%
echo       Log    : %LOG%
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" ^
  -buildTarget win64 ^
  -buildWindows64Player "%OUT_DIR%\%GAME%.exe" ^
  -logFile "%LOG%"
if errorlevel 1 goto :err_build

echo [3/4] Writing version.txt and removing files that must not be shipped
(
  echo %GAME% v%VERSION%
  echo Build date : %BUILD_DATE%
  echo Commit     : %COMMIT%
) > "%OUT_DIR%\version.txt"
for /d %%X in ("%OUT_DIR%\*_BurstDebugInformation_DoNotShip") do rmdir /s /q "%%X"
for /d %%X in ("%OUT_DIR%\*_BackUpThisFolder_ButDontShipItWithYourGame") do rmdir /s /q "%%X"

echo [4/4] Zipping to %ZIP%
if exist "%ZIP%" del /q "%ZIP%"
powershell -NoProfile -Command "Compress-Archive -Path '%OUT_DIR%' -DestinationPath '%ZIP%' -Force"
if errorlevel 1 goto :err_zip

echo.
echo Done: %GAME% v%VERSION%  (%BUILD_DATE%, commit %COMMIT%)
echo       Folder : %OUT_DIR%
echo       Zip    : %ZIP%
set "RC=0"
goto :finish

:err_engine
echo [ERROR] Unity.exe not found. Check UNITY: %UNITY%
goto :finish

:err_project
echo [ERROR] Not a Unity project folder. Check PROJECT: %PROJECT%
goto :finish

:err_version
echo [ERROR] Could not work out the version. Pass it: package_unity.bat 0.1.4
goto :finish

:err_out
echo [ERROR] Could not create the output folder: %OUT_DIR%
goto :finish

:err_build
echo [FAILED] Open the log and search for the FIRST error:
echo          %LOG%
echo          Is the Unity Editor still open with this project?
goto :finish

:err_zip
echo [FAILED] Could not create the zip: %ZIP%
goto :finish

:finish
if defined PAUSE_AT_END pause
exit /b %RC%
