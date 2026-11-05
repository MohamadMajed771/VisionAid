import json
import time
import subprocess
import re
import requests

from collections import Counter


MODEL_NAME = "llama3.2"

OLLAMA_URL = "http://127.0.0.1:11434/api/chat"
OLLAMA_LOAD_URL = "http://127.0.0.1:11434/api/generate"

# Reuse HTTP connections between requests
session = requests.Session()


# ------------------------------------
# 1. Load Llama 3.2
# ------------------------------------

def load_model():

    print("Loading Llama 3.2 into memory...")

    request_body = {
        "model": MODEL_NAME,
        "prompt": "",
        "keep_alive": -1
    }

    start_time = time.perf_counter()

    try:
        response = session.post(
            OLLAMA_LOAD_URL,
            json=request_body,
            timeout=120
        )

        response.raise_for_status()

        elapsed_time = time.perf_counter() - start_time

        print(
            f"Llama 3.2 loaded successfully in "
            f"{elapsed_time:.2f} seconds."
        )

    except requests.RequestException as error:
        print("Failed to load Llama 3.2:", error)
        raise


# ------------------------------------
# 2. Generate VisionAid answer
# ------------------------------------

def generate_answer(question, detected_objects, ocr_results):

    system_prompt = """
You are VisionAid, a voice assistant for visually impaired people.

Answer the question using ONLY the supplied observations.

RULES:
1. Never invent objects, text, colors, or distances.
2. Never say the environment is safe.
3. Summarize repeated objects instead of listing each one.
4. Use natural language, not technical object labels.
5. Mention only the most relevant objects and approximate distances.
6. If OCR is empty, NEVER mention visible text.
7. OCR text is independent of detected objects unless explicitly linked.
8. Answer in one short sentence, or two if necessary.
9. Do not mention JSON, confidence scores, or technical details.
10. If the observations are insufficient, say so.
11. The nearest distance refers to the nearest detected object,
    not necessarily to every object mentioned.

Example:
Objects: 3 sofas, 2 plants, 1 television.
Nearest distance: 4.6 meters.
OCR: none.
Question: What is in front of me?

Answer:
"I can detect several sofas, plants, and a television ahead.
The nearest detected object is approximately 4.6 meters away."
"""

    # ------------------------------------
    # Count detected objects
    # ------------------------------------

    object_counts = Counter(
        obj["label"]
        for obj in detected_objects
        if obj.get("label")
    )

    # ------------------------------------
    # Find nearest detected object
    # ------------------------------------

    valid_distances = [
        obj["depth"]
        for obj in detected_objects
        if isinstance(obj.get("depth"), (int, float))
        and not isinstance(obj.get("depth"), bool)
        and obj["depth"] > 0
    ]

    nearest_distance = (
        round(min(valid_distances), 1)
        if valid_distances
        else None
    )

    # ------------------------------------
    # Extract OCR text
    # ------------------------------------

    recognized_texts = [
        item["extractedText"]
        for item in ocr_results
        if item.get("extractedText")
    ]

    # ------------------------------------
    # Prepare observations
    # ------------------------------------

    observations = {
        "question": question,
        "objects": dict(object_counts),
        "nearestDistanceMeters": nearest_distance,
        "ocrTexts": recognized_texts
    }

    # ------------------------------------
    # Prepare Ollama request
    # ------------------------------------

    request_body = {
        "model": MODEL_NAME,
        "messages": [
            {
                "role": "system",
                "content": system_prompt
            },
            {
                "role": "user",
                "content": json.dumps(
                    observations,
                    ensure_ascii=False
                )
            }
        ],
        "stream": False,
        "options": {
            "temperature": 0,
            "num_predict": 80,
            "num_ctx": 2048
        },
        "keep_alive": -1
    }

    # ------------------------------------
    # Send request to Ollama
    # ------------------------------------

    start_time = time.perf_counter()

    try:
        response = session.post(
            OLLAMA_URL,
            json=request_body,
            timeout=120
        )

        response.raise_for_status()

        result = response.json()

    except requests.RequestException as error:
        print("Error connecting to Ollama:", error)
        return None

    except ValueError as error:
        print("Invalid JSON response from Ollama:", error)
        return None

    elapsed_time = time.perf_counter() - start_time

    # ------------------------------------
    # Extract generated answer
    # ------------------------------------

    message = result.get("message", {})

    answer = message.get("content", "").strip()

    # Remove reasoning tags if they appear
    answer = re.sub(
        r"<think>.*?</think>",
        "",
        answer,
        flags=re.DOTALL
    ).strip()

    if "<think>" in answer:
        answer = ""

    # ------------------------------------
    # Extract Ollama performance metrics
    # ------------------------------------

    ollama_total_seconds = round(
        result.get("total_duration", 0) / 1_000_000_000,
        2
    )

    model_load_seconds = round(
        result.get("load_duration", 0) / 1_000_000_000,
        2
    )

    prompt_processing_seconds = round(
        result.get("prompt_eval_duration", 0) / 1_000_000_000,
        2
    )

    generation_seconds = round(
        result.get("eval_duration", 0) / 1_000_000_000,
        2
    )

    # ------------------------------------
    # Return result
    # ------------------------------------

    return {
        "model": MODEL_NAME,
        "answer": answer,

        # Total Python HTTP request time
        "processing_time_seconds": round(
            elapsed_time,
            2
        ),

        # Total internal Ollama duration
        "ollama_total_seconds": ollama_total_seconds,

        # Model performance
        "model_load_seconds": model_load_seconds,
        "prompt_processing_seconds": prompt_processing_seconds,
        "generation_seconds": generation_seconds,

        # Token information
        "prompt_tokens": result.get(
            "prompt_eval_count",
            0
        ),
        "total_tokens": result.get(
            "eval_count",
            0
        )
    }


# ------------------------------------
# 3. Check GPU usage
# ------------------------------------

def check_gpu_usage():

    print("\n===== GPU INFORMATION =====")

    try:
        result = subprocess.run(
            [
                "nvidia-smi",
                "--query-gpu=name,memory.used,memory.total",
                "--format=csv,noheader"
            ],
            capture_output=True,
            text=True,
            check=True
        )

        print(result.stdout.strip())

    except (FileNotFoundError, subprocess.CalledProcessError):
        print("NVIDIA GPU information unavailable.")


# ------------------------------------
# 4. Check Ollama processor
# ------------------------------------

def check_ollama_processor():

    print("\n===== OLLAMA PROCESSOR =====")

    try:
        result = subprocess.run(
            ["ollama", "ps"],
            capture_output=True,
            text=True,
            check=True
        )

        print(result.stdout.strip())

    except (FileNotFoundError, subprocess.CalledProcessError):
        print("Unable to retrieve Ollama processor information.")


# ------------------------------------
# 5. Standalone test
# ------------------------------------

if __name__ == "__main__":

    detected_objects = [
        {
            "label": "traffic light",
            "confidence": 0.96,
            "depth": 4.89,
            "depthUnit": "meters"
        },
        {
            "label": "car",
            "confidence": 0.95,
            "depth": 5.77,
            "depthUnit": "meters"
        },
        {
            "label": "person",
            "confidence": 0.94,
            "depth": 7.38,
            "depthUnit": "meters"
        }
    ]

    ocr_results = [
        {
            "extractedText": "STOP",
            "confidence": 0.94
        }
    ]

    question = "What is in front of me?"

    print("\n===== VISIONAID LLAMA 3.2 TEST =====")

    # Preload the model
    load_model()

    print("\nQuestion:", question)

    result = generate_answer(
        question,
        detected_objects,
        ocr_results
    )

    if result is not None:

        print("\n===== LLM RESULT =====")

        print("Model:", result["model"])

        print("Answer:", result["answer"])

        print(
            "Processing time:",
            result["processing_time_seconds"],
            "seconds"
        )

        print(
            "Ollama internal duration:",
            result["ollama_total_seconds"],
            "seconds"
        )

        print(
            "Model loading time:",
            result["model_load_seconds"],
            "seconds"
        )

        print(
            "Prompt processing time:",
            result["prompt_processing_seconds"],
            "seconds"
        )

        print(
            "Generation time:",
            result["generation_seconds"],
            "seconds"
        )

        print(
            "Prompt tokens:",
            result["prompt_tokens"]
        )

        print(
            "Generated tokens:",
            result["total_tokens"]
        )

        if not result["answer"]:
            print(
                "WARNING: The model did not produce "
                "a usable answer."
            )

        check_gpu_usage()
        check_ollama_processor()

    else:
        print("LLM test failed.")