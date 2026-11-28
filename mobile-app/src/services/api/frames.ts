
import { File } from "expo-file-system";

import { apiRequest } from "./client";
import type {
  FrameAnalysisResponse,
} from "../../types/frame";

export const framesApi = {
  async upload(
    sessionId: number,
    imageUri: string,
    token: string,
    audioUri?: string
  ): Promise<FrameAnalysisResponse> {
    const formData = new FormData();

    const imageFile = new File(imageUri);

    if (!imageFile.exists) {
      throw new Error(
        "Captured image file was not found."
      );
    }

    formData.append(
      "Image",
      imageFile,
      `frame_${Date.now()}.jpg`
    );

    // Used to identify the intended session.
    // We will validate this in the backend in Stage 8.
    formData.append("SessionId", String(sessionId));

    if (audioUri) {
      const audioFile = new File(audioUri);

      if (!audioFile.exists) {
        throw new Error(
          "Recorded WAV file was not found."
        );
      }
      
      formData.append(
        "Question",
        audioFile,
        `question_${Date.now()}.wav`
      );
    }

    return apiRequest<FrameAnalysisResponse>(
      "/api/analyse/frame",
      {
        method: "POST",
        body: formData,
      },
      token,
      300000
    );
  },
};
