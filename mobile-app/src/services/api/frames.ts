
import { apiRequest } from "./client";
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
};
