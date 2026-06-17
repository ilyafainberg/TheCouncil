@echo off
setlocal EnableExtensions EnableDelayedExpansion
:: ============================================================================
::  apply-update.cmd — update applier helper for The Council ("Option A").
::
::  Runs as a SEPARATE process from the app so it can replace the (otherwise
::  locked) running .exe. Launched by UpdateChecker.DownloadAndApplyAsync with:
::    %1 = PID of the app to wait for
::    %2 = install kind: "portable" | "installer"
::    %3 = path to the downloaded asset zip
::    %4 = install directory (where the app currently lives)
::    %5 = exe file name to relaunch (TheCouncil.exe)
:: ============================================================================

set "APP_PID=%~1"
set "KIND=%~2"
set "ASSET=%~3"
set "INSTALL_DIR=%~4"
set "APP_EXE=%~5"

:: --- 1. Wait for the app to exit (poll the PID up to ~30s) -------------------
echo Waiting for The Council (PID %APP_PID%) to close...
set /a _tries=0
:waitloop
tasklist /FI "PID eq %APP_PID%" 2>nul | find "%APP_PID%" >nul
if not errorlevel 1 (
    set /a _tries+=1
    if !_tries! geq 60 goto :giveup
    ping -n 2 127.0.0.1 >nul
    goto :waitloop
)

if /I "%KIND%"=="installer" goto :installer

:: --- 2a. PORTABLE: extract zip contents over the install directory ----------
echo Applying portable update...
set "STAGE=%TEMP%\TheCouncil-stage-%RANDOM%"
mkdir "%STAGE%" >nul 2>&1
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Expand-Archive -LiteralPath '%ASSET%' -DestinationPath '%STAGE%' -Force"
if errorlevel 1 goto :fail

:: Copy everything from the stage root into the install dir, overwriting.
robocopy "%STAGE%" "%INSTALL_DIR%" /E /IS /IT /NFL /NDL /NJH /NJS /R:2 /W:1 >nul
:: robocopy exit codes 0-7 are success; 8+ are failures.
if %ERRORLEVEL% GEQ 8 goto :fail
goto :relaunch

:: --- 2b. INSTALLER: extract setup.exe and run it silently -------------------
:installer
echo Applying installer update...
set "STAGE=%TEMP%\TheCouncil-stage-%RANDOM%"
mkdir "%STAGE%" >nul 2>&1
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Expand-Archive -LiteralPath '%ASSET%' -DestinationPath '%STAGE%' -Force"
if errorlevel 1 goto :fail

:: Inno requests UAC itself and relaunches the app on finish.
for %%F in ("%STAGE%\*setup*.exe") do (
    "%%~fF" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
    goto :cleanup
)
goto :fail

:: --- 3. Relaunch (portable path) --------------------------------------------
:relaunch
echo Restarting The Council...
start "" "%INSTALL_DIR%\%APP_EXE%"
goto :cleanup

:: --- 4. Cleanup -------------------------------------------------------------
:cleanup
if defined STAGE rmdir /S /Q "%STAGE%" >nul 2>&1
del /Q "%ASSET%" >nul 2>&1
endlocal
exit /b 0

:giveup
echo Timed out waiting for the app to close. Aborting update.
endlocal
exit /b 1

:fail
echo Update failed. Your existing install was left untouched (or partially
echo updated for portable). Re-download the latest release manually if needed.
if defined STAGE rmdir /S /Q "%STAGE%" >nul 2>&1
endlocal
exit /b 1
