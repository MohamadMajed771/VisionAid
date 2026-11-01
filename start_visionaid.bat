@echo off
setlocal

title VisionAid Launcher

cd /d "%~dp0"

REM Python environments
set "MAIN_PYTHON=C:\Users\HP\anaconda3\python.exe"
set "OCR_PYTHON=%~dp0ai-models\ocr-env\Scripts\python.exe"

REM Check Python environments
if not exist "%MAIN_PYTHON%" (
    echo ERROR: Main Python environment not found.
    pause
    exit /b 1
)

if not exist "%OCR_PYTHON%" (
    echo ERROR: OCR Python environment not found.
    pause
    exit /b 1
)

cd /d "%~dp0ai-models"

echo ========================================
echo       Starting VisionAid Servers
echo ========================================

echo Starting PaddleOCR on port 8001...

start "VisionAid - PaddleOCR 8001" cmd /k ""%OCR_PYTHON%" -m uvicorn ocr_server:app --host 127.0.0.1 --port 8001"

echo Starting Main API on port 8000...

start "VisionAid - Main API 8000" cmd /k ""%MAIN_PYTHON%" -m uvicorn main:app --host 127.0.0.1 --port 8000"

echo.
echo Both server processes have been launched.
echo Wait for both servers to finish loading.
echo.
echo Main API: http://127.0.0.1:8000
echo OCR API:  http://127.0.0.1:8001

pause
