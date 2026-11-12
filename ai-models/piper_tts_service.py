
"""
VisionAid - Text-to-Speech Service

Provides multilingual speech synthesis using Piper TTS.

Supported languages:
    en: English
    fr: French

Models are loaded once and reused throughout the
FastAPI application's lifetime.
"""

import io
import threading
import time
import wave
from pathlib import Path

from piper import PiperVoice


BASE_DIR = Path(__file__).resolve().parent

VOICE_PATHS = {
    "en": BASE_DIR / "en_US-lessac-medium.onnx",
    "fr": BASE_DIR / "fr_FR-siwis-medium.onnx",
}

# Cached voice models
_voices = {}

# Protect model initialization and synthesis
_tts_lock = threading.RLock()


def load_tts_models():
    """
    Load all supported Piper voices once.

    Reuse cached voices on subsequent calls.
    """
    with _tts_lock:
        for language, model_path in VOICE_PATHS.items():

            if language in _voices:
                print(f"Reusing cached TTS voice: {language}")
                continue

            if not model_path.is_file():
                raise FileNotFoundError(
                    f"TTS model not found: {model_path}"
                )

            start = time.perf_counter()

            voice = PiperVoice.load(str(model_path))

            _voices[language] = voice

            elapsed = time.perf_counter() - start

            print(
                f"Piper TTS ({language}) loaded "
                f"in {elapsed:.2f} seconds"
            )

    return _voices


def synthesize_speech(text: str, language: str = "en") -> bytes:
    """
    Convert text into WAV audio in memory.

    Returns:
        WAV file contents as bytes.

    Raises:
        ValueError: Unsupported language or empty text.
        RuntimeError: TTS models have not been loaded.
    """
    if not text or not text.strip():
        raise ValueError("Cannot synthesize empty text.")

    language = language.strip().lower()

    if language not in VOICE_PATHS:
        raise ValueError(
            f"Unsupported TTS language: {language}"
        )

    with _tts_lock:
        voice = _voices.get(language)

        if voice is None:
            raise RuntimeError(
                "TTS models are not loaded. "
                "Call load_tts_models() at startup."
            )

        start = time.perf_counter()

        audio_buffer = io.BytesIO()

        with wave.open(audio_buffer, "wb") as wav_file:
            voice.synthesize_wav(text.strip(), wav_file)

        wav_bytes = audio_buffer.getvalue()

        elapsed = time.perf_counter() - start

    print(
        f"Piper TTS ({language}): "
        f"{elapsed:.3f} seconds, "
        f"{len(wav_bytes)} bytes"
    )

    return wav_bytes


def warmup_tts_models():
    """Warm up the cached voices after startup."""
    samples = {
        "en": "VisionAid is ready.",
        "fr": "VisionAid est prêt.",
    }

    for language, text in samples.items():
        synthesize_speech(text, language)

    print("Piper TTS models warmed up successfully.")
