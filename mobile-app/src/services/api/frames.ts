

import { File } from "expo-file-system";

import { apiRequest } from "./client";
import type { FrameAnalysisResponse } from "../../types/frame";

export const framesApi = {
  async upload(
    sessionId: number,
    imageUri: string,
    token: string,
    question?: string,
    audioUri?: string

  ): Promise<FrameAnalysisResponse> {
    const formData = new FormData();

    // Create a real File from the camera's local URI.
    const imageFile = new File(imageUri);

    if (!imageFile.exists) {
      throw new Error("Captured image file was not found.");
    }

    formData.append(
      "Image",
      imageFile,
      `frame_${Date.now()}.jpg`
    );
    if (question?.trim()) {
      formData.append("Question", question.trim());
    }
    if (audioUri) {
      const audioFile = new File(audioUri);
        
      formData.append(
        "Audio",
        audioFile,
        `question_${Date.now()}.wav`
      );
    }
    return apiRequest<FrameAnalysisResponse>(
      `/api/sessions/${sessionId}/frames`,
      {
        method: "POST",
        body: formData,
      },
      token,
      300000
    );
  },
};

/*import { apiRequest } from "./client";
import type { FrameAnalysisResponse } from "../../types/frame";

export const framesApi = {
  async upload(
    sessionId: number,
    imageUri: string,
    token: string,
    question?: string
  ): Promise<FrameAnalysisResponse> {

    // Multipart/form-data supports uploading files.
    const formData = new FormData();

    // React Native file representation.
    formData.append("Image", {
      uri: imageUri,
      name: `frame_${Date.now()}.jpg`,
      type: "image/jpeg",
    } as unknown as Blob);

    // Include a question only when provided.
    if (question?.trim()) {
      formData.append("Question", question.trim());
    }

    return apiRequest<FrameAnalysisResponse>(
      `/api/sessions/${sessionId}/frames`,
      {
        method: "POST",
        body: formData,
      },
      token,
      300000
    );
  },
};*/
