@echo off
setlocal EnableExtensions
rem ===================================================================
rem  LAUNCH:ER Day 7 - Unity player build (Windows / Win64)
rem  Usage   : double-click, or  package_unity.bat [output folder]
rem            e.g. package_unity.bat D:\Builds\v0.1.1
rem  Output  : <output folder>\<Game>.exe ...   log: <output folder>.log
rem  CLOSE the Unity Editor for this project first, or the build fails.
rem  Scenes  : the scene list in Build Profiles (Build Settings).
rem  Day 8 adds a version number, folder name and zip on top of this.
rem  Comments are in English on purpose: Korean text in .bat files
rem  can break depending on the file encoding / console code page.
rem ===================================================================

rem ---- 1) Change these three lines for your PC ----------------------
rem      Replace 6000.0.XXf1 with your editor version folder in Unity Hub.
if not defined UNITY    set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
if not defined PROJECT  set "PROJECT=C:\Users\user\Documents\GitHub\VoidConverter"
if not defined OUT_DIR  set "OUT_DIR=C:\Builds\v0.2"
rem -------------------------------------------------------------------

set "RC=1"
set "PAUSE_AT_END=1"
if "%~1"=="" goto :args_done
set "OUT_DIR=%~1"
set "PAUSE_AT_END="
:args_done
if "%OUT_DIR:~-1%"=="\" set "OUT_DIR=%OUT_DIR:~0,-1%"
if "%PROJECT:~-1%"=="\" set "PROJECT=%PROJECT:~0,-1%"
set "LOG=%OUT_DIR%.log"

if not exist "%UNITY%" goto :err_engine
if not exist "%PROJECT%\Assets" goto :err_project
for %%P in ("%PROJECT%") do set "GAME=%%~nxP"
if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"
if not exist "%OUT_DIR%" goto :err_out

echo [1/2] Building %GAME% [Win64]
echo       Output : %OUT_DIR%
echo       Log    : %LOG%

"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" ^
  -buildTarget win64 ^
  -buildWindows64Player "%OUT_DIR%\%GAME%.exe" ^
  -logFile "%LOG%"
if errorlevel 1 goto :err_build

echo [2/2] Done: %OUT_DIR%
echo       Run %GAME%.exe there, then zip that whole folder.
set "RC=0"
goto :finish

:err_engine
echo [ERROR] Unity.exe not found. Check UNITY: %UNITY%
goto :finish

:err_project
echo [ERROR] Not a Unity project folder. Check PROJECT: %PROJECT%
goto :finish

:err_out
echo [ERROR] Could not create the output folder: %OUT_DIR%
goto :finish

:err_build
echo [FAILED] Open the log and search for the FIRST error:
echo          %LOG%
echo          Is the Unity Editor still open with this project?
goto :finish

:finish
if defined PAUSE_AT_END pause
exit /b %RC%
