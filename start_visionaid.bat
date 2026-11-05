@echo off
setlocal

title VisionAid Launcher

REM Go to the VisionAid project directory
cd /d "%~dp0"

REM ========================================
REM Python environments
REM ========================================

set "MAIN_PYTHON=C:\Users\96170\AppData\Local\Programs\Python\Python311\python.exe"
set "OCR_PYTHON=%~dp0ai-models\ocr-env\Scripts\python.exe"

REM ========================================
REM Check Python environments
REM ========================================

if not exist "%MAIN_PYTHON%" (
    echo ERROR: Main Python environment not found.
    echo Path: %MAIN_PYTHON%
    pause
    exit /b 1
)

if not exist "%OCR_PYTHON%" (
    echo ERROR: OCR Python environment not found.
    echo Path: %OCR_PYTHON%
    pause
    exit /b 1
)

REM ========================================
REM Check Ollama
REM ========================================

echo Checking Ollama...

curl -s --max-time 5 http://127.0.0.1:11434/api/tags >nul 2>&1

if errorlevel 1 (
    echo.
    echo ERROR: Ollama is not running.
    echo Please start Ollama before launching VisionAid.
    pause
    exit /b 1
)

echo Ollama is running.

REM ========================================
REM Navigate to AI models directory
REM ========================================

cd /d "%~dp0ai-models"

echo.
echo ========================================
echo       Starting VisionAid Servers
echo ========================================
echo.

REM ========================================
REM Start PaddleOCR server
REM ========================================

echo Starting PaddleOCR on port 8001...

start "VisionAid - PaddleOCR 8001" cmd /k ""%OCR_PYTHON%" -m uvicorn ocr_server:app --host 127.0.0.1 --port 8001"

REM ========================================
REM Start Main FastAPI server
REM ========================================

echo Starting Main API on port 8000...

start "VisionAid - Main API 8000" cmd /k ""%MAIN_PYTHON%" -m uvicorn main:app --host 127.0.0.1 --port 8000"

REM ========================================
REM Finished
REM ========================================

echo.
echo ========================================
echo       VisionAid Servers Launched
echo ========================================
echo.
echo PaddleOCR API: http://127.0.0.1:8001
echo Main API:      http://127.0.0.1:8000
echo Swagger UI:    http://127.0.0.1:8000/docs
echo Ollama API:    http://127.0.0.1:11434
echo.
echo AI Models:
echo - RT-DETRv2
echo - UniDepth V2 Small
echo - Whisper Small
echo - PaddleOCR
echo - Llama 3.2
echo.
echo Wait for both servers to finish loading.
echo Check both terminal windows for errors.
echo.

pause