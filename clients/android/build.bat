@echo off
setlocal

set "JAVA_HOME=C:\Program Files\Android\Android Studio\jbr"
cd /d "%~dp0"
call gradlew.bat assembleDebug %*

if %ERRORLEVEL% EQU 0 (
    echo.
    echo BUILD SUCCESSFUL
    echo APK: %~dp0app\build\outputs\apk\debug\YepList.apk
) else (
    echo.
    echo BUILD FAILED
)
