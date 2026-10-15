import torch
from PIL import Image, ImageDraw
from transformers import RTDetrImageProcessor, RTDetrV2ForObjectDetection

# Load our downloaded model
model_path = "ai-models/rtdetr-v2"

processor = RTDetrImageProcessor.from_pretrained(model_path)
model = RTDetrV2ForObjectDetection.from_pretrained(model_path)
model = model.to("cuda")
model.eval()

# Load image from Downloads
image_path = r"C:\Users\96170\Downloads\test.jpg"
image = Image.open(image_path).convert("RGB")

# Prepare image
inputs = processor(images=image, return_tensors="pt").to("cuda")

# Detect objects
with torch.no_grad():
    outputs = model(**inputs)

# Convert predictions into bounding boxes
results = processor.post_process_object_detection(
    outputs,
    target_sizes=torch.tensor([image.size[::-1]]),
    threshold=0.5
)[0]

# Draw bounding boxes
draw = ImageDraw.Draw(image)

for score, label, box in zip(
    results["scores"],
    results["labels"],
    results["boxes"]
):
    name = model.config.id2label[label.item()]
    confidence = score.item()

    x1, y1, x2, y2 = box.tolist()

    draw.rectangle((x1, y1, x2, y2), outline="red", width=2)
    draw.text((x1, max(0, y1 - 12)), f"{name}: {confidence:.2f}", fill="red")

    print(f"Detected: {name} | Confidence: {confidence:.2f}")

# Save the result
image.save("ai-models/result.jpg")

print("Detection completed! Result saved to ai-models/result.jpg")