@echo off
REM ALT_LAS Engine - Windows Launcher
REM This script launches the game on Windows systems

echo ========================================
echo   ALT_LAS Engine - Windows Launcher
echo ========================================
echo.

REM Check Python installation
python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python is not installed or not in PATH
    echo Please install Python 3.8+ from https://www.python.org/
    pause
    exit /b 1
)

REM Navigate to script directory
cd /d "%~dp0"

REM Run the game
echo Starting ALT_LAS Engine...
echo.
python main.py

REM Keep window open if there's an error
if errorlevel 1 (
    echo.
    echo ========================================
    echo   Game exited with an error
    echo ========================================
    pause
)
