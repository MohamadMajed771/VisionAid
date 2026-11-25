import re
import math


# ------------------------------------
# 1. Alert configuration
# ------------------------------------

OCR_ALERT_CONFIDENCE = 0.80

IMPORTANT_KEYWORDS = {
    "EXIT",
    "STOP",
    "DANGER",
    "WARNING",
    "CAUTION",
    "EMERGENCY",
    "DO NOT ENTER",
    "NO ENTRY",
    "FIRE",
    "SORTIE",
    "ATTENTION",
    "URGENCE",
    "INTERDIT"
}


# ------------------------------------
# 2. Identify important text
# ------------------------------------

def is_important_text(text):

    text = text.upper().strip()

    for keyword in IMPORTANT_KEYWORDS:

        pattern = r"\b" + re.escape(keyword) + r"\b"

        if re.search(pattern, text):
            return True

    return False


# ------------------------------------
# 3. Generate proximity alert
# ------------------------------------

def generate_proximity_alert(obstacle, language):

    if obstacle is None:
        return None

    distance = obstacle["distance"]
    level = obstacle["level"]

    if language == "fr":

        if level == "critical":
            return (
                "Arrêtez-vous ! Un obstacle semble être "
                f"très proche devant vous, à environ "
                f"{distance:.1f} mètre."
            )

        return (
            "Attention ! Un obstacle semble être "
            f"devant vous, à environ {distance:.1f} mètre."
        )

    if level == "critical":
        return (
            "Stop walking! There appears to be an "
            f"obstacle approximately {distance:.1f} "
            "meters in front of you."
        )

    return (
        "Caution! There appears to be an obstacle "
        f"approximately {distance:.1f} meters "
        "in front of you."
    )


# ------------------------------------
# 4. Generate important text alert
# ------------------------------------

def generate_ocr_alert(item, language):

    text = item["text"].strip()
    confidence = float(item["confidence"])

    if not math.isfinite(confidence):
        return None

    if confidence < OCR_ALERT_CONFIDENCE:
        return None

    if not is_important_text(text):
        return None

    distance = item.get("distance")
    direction = item.get("direction")

    if language == "fr":

        directions = {
            "left": "à votre gauche",
            "right": "à votre droite",
            "center": "devant vous"
        }

        message = f'Attention ! Texte détecté : "{text}"'

        if distance is not None:
            message += f", à environ {distance:.1f} mètres"

        if direction in directions:
            message += f", {directions[direction]}"

        return message + "."

    directions = {
        "left": "to your left",
        "right": "to your right",
        "center": "in front of you"
    }

    message = f'Attention! Detected text: "{text}"'

    if distance is not None:
        message += f", approximately {distance:.1f} meters"

    if direction in directions:
        message += f" {directions[direction]}"

    return message + "."


# ------------------------------------
# 5. Generate automatic alerts
# ------------------------------------

def generate_automatic_alert(
    combined_result,
    language="en"
):

    messages = []

    # Priority 1: Nearby obstacles
    obstacle = combined_result.get("nearby_obstacle")

    proximity_alert = generate_proximity_alert(
        obstacle,
        language
    )

    if proximity_alert:
        messages.append(proximity_alert)

    # Priority 2: Important OCR text
    ocr_results = combined_result.get(
        "ocr_spatial_results",
        []
    )

    for item in ocr_results:

        ocr_alert = generate_ocr_alert(
            item,
            language
        )

        if ocr_alert and ocr_alert not in messages:
            messages.append(ocr_alert)

    # No automatic alert
    if not messages:
        return None

    # Combine all alert messages
    return " ".join(messages)



# ------------------------------------
# 6. Prepare automatic alert evidence
# for Llama 3.2
# ------------------------------------

def prepare_alert_evidence(combined_result):

    nearby_obstacle = combined_result.get(
        "nearby_obstacle"
    )

    ocr_results = combined_result.get(
        "ocr_spatial_results",
        []
    )

    important_ocr = []

    # Select important OCR observations
    for item in ocr_results:

        text = item["text"].strip()
        confidence = float(item["confidence"])

        if not math.isfinite(confidence):
            continue

        if confidence < OCR_ALERT_CONFIDENCE:
            continue

        if not is_important_text(text):
            continue

        important_ocr.append({
            "text": text,
            "confidence": confidence,
            "distance": item.get("distance"),
            "direction": item.get("direction")
        })

    # No important event: do not call Llama
    if nearby_obstacle is None and not important_ocr:
        return None

    # Prepare detected objects
    detected_objects = []

    for obj in combined_result.get("objects", []):

        detected_objects.append({
            "label": obj.get("name"),
            "confidence": obj.get("confidence"),
            "distance": obj.get("estimated_distance_m"),
            "bounding_box": obj.get("bounding_box")
        })

    # Return observations for Llama
    return {
        "detected_objects": detected_objects,
        "important_ocr": important_ocr,
        "nearby_obstacle": nearby_obstacle
    }