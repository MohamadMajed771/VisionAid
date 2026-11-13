
import { apiRequest } from "./client";
import type { SessionResponse } from "../../types/session";

export const sessionsApi = {
  // Start a new assistance session.
  create(token: string): Promise<SessionResponse> {
    return apiRequest<SessionResponse>(
      "/api/sessions",
      { method: "POST" },
      token
    );
  },

  // Retrieve the authenticated user's active session.
  getActive(token: string): Promise<SessionResponse> {
    return apiRequest<SessionResponse>(
      "/api/sessions/active",
      { method: "GET" },
      token
    );
  },

  // End the given session.
  end(sessionId: number, token: string): Promise<void> {
    return apiRequest<void>(
      `/api/sessions/${sessionId}/end`,
      { method: "POST" },
      token
    );
  },
};
