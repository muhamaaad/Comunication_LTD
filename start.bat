@echo off
@chcp 65001 >/dev/null 2>NUL
@cls
title Comunication_LTD Launcher
echo ============================================
echo   Comunication_LTD - Startup Script
echo ============================================
echo.

:: Ask for MySQL root password
set /p "MYSQL_PASS=Enter MySQL root password: "

:: Update password in both settings.py
python update_password.py "%MYSQL_PASS%"
if %errorlevel% neq 0 (
    echo ERROR: Failed to update settings.py files
    pause
    exit /b 1
)

echo.
echo --- Running migrations [vulnerable] ---
cd vulnerable
python manage.py migrate --run-syncdb
if %errorlevel% neq 0 (
    echo ERROR: Vulnerable migrations failed. Check your MySQL password.
    cd ..
    pause
    exit /b 1
)

echo --- Seeding data [vulnerable] ---
python manage.py seed_data
cd ..

echo.
echo --- Running migrations [secure] ---
cd secure
python manage.py migrate --run-syncdb
if %errorlevel% neq 0 (
    echo ERROR: Secure migrations failed. Check your MySQL password.
    cd ..
    pause
    exit /b 1
)

echo --- Seeding data [secure] ---
python manage.py seed_data
cd ..

echo.
echo ============================================
echo   Starting servers...
echo   Vulnerable: http://localhost:8000
echo   Secure:     http://localhost:9000
echo ============================================
echo   Press Ctrl+C in either window to stop it.
echo ============================================
echo.

start "Vulnerable [port 8000]" cmd /k "cd vulnerable && python manage.py runserver 8000"
start "Secure [port 9000]" cmd /k "cd secure && python manage.py runserver 9000"

echo Both servers launched. You can close this window.
pause
