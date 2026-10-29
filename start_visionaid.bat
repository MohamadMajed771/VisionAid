
@echo off
setlocal

title VisionAid Launcher

REM Go to the folder containing this batch file.
cd /d "%~dp0"

REM Check that the required files exist.
if not exist "ai-models\main.py" (
    echo ERROR: ai-models\main.py not found.
    pause
    exit /b 1
)

if not exist "ai-models\ocr_server.py" (
    echo ERROR: ai-models\ocr_server.py not found.
    pause
    exit /b 1
)

if not exist "ai-models\ocr-env\Scripts\python.exe" (
    echo ERROR: OCR virtual environment not found.
    pause
    exit /b 1
)

REM Enter the AI models directory.
cd /d "%~dp0ai-models"

echo ========================================
echo       Starting VisionAid Servers
echo ========================================

REM Start the PaddleOCR GPU server.
echo Starting PaddleOCR on port 8001...

start "VisionAid - PaddleOCR 8001" cmd /k ".\ocr-env\Scripts\python.exe -m uvicorn ocr_server:app --host 127.0.0.1 --port 8001"

REM Start the main FastAPI backend.
echo Starting main backend on port 8000...

start "VisionAid - Main API 8000" cmd /k "python -m uvicorn main:app --host 127.0.0.1 --port 8000"

echo.
echo Both server processes have been launched.
echo Wait for both servers to finish loading.
echo.
echo Main API: http://127.0.0.1:8000
echo OCR API:  http://127.0.0.1:8001
echo.
echo Close both server windows to stop VisionAid.
echo.
pause
