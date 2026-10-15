import torch
import depth_pro
import inspect
from dataclasses import replace

# 1. Select NVIDIA GPU
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

# 2. Get the default Depth Pro configuration
default_config = inspect.signature(
    depth_pro.create_model_and_transforms
).parameters["config"].default

# 3. Set our downloaded model checkpoint
config = replace(
    default_config,
    checkpoint_uri="ai-models/depth-pro/depth_pro.pt"
)

print("Configuration ready!")
print("Checkpoint:", config.checkpoint_uri)
print("Device:", device)

# 4. Load Depth Pro
try:
    print("Loading Depth Pro...")

    model, transform = depth_pro.create_model_and_transforms(
        config=config,
        device=device
    )

    model.eval()

    print("Depth Pro loaded successfully!")

    # 5. Load the test image
    image_path = r"C:\Users\96170\Downloads\test.jpg"

    image, _, f_px = depth_pro.load_rgb(image_path)

    # 6. Prepare image for inference
    image = transform(image).to(device)

    print("Processing image...")

    # 7. Estimate depth
    with torch.no_grad():
        prediction = model.infer(image, f_px=f_px)

    depth = prediction["depth"]

    # 8. Display estimated depth values
    print("Depth estimation completed!")
    print("Minimum estimated depth:", depth.min().item(), "meters")
    print("Maximum estimated depth:", depth.max().item(), "meters")
    import numpy as np
    from PIL import Image

    # Convert depth values to a NumPy array
    depth_array = depth.detach().cpu().numpy()

    # Normalize values between 0 and 255
    depth_normalized = (
        (depth_array - depth_array.min())
        / (depth_array.max() - depth_array.min() + 1e-8)
        * 255
    ).astype(np.uint8)

    # Save depth visualization
    depth_image = Image.fromarray(depth_normalized)
    depth_image.save("ai-models/depth_result.png")

    print("Depth map saved to ai-models/depth_result.png")

except torch.cuda.OutOfMemoryError:
    print("ERROR: Not enough GPU memory.")
    print("We will try a memory-saving approach.")

except Exception as e:
    print("ERROR:", e)