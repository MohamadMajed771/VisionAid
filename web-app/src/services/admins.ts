
import { apiRequest } from "./api";
import type {
  AdminAuthResponse,
  AdminLoginRequest,
  AdminRegisterRequest,
  AdminUserResponse,
  AccessRequestResponse,
  SessionResponse,
  SessionHistoryPage,
} from "../types/admin";

export const adminApi = {
  register(request: AdminRegisterRequest) {
    return apiRequest<AdminAuthResponse>("/admins/register", {
      method: "POST",
      body: JSON.stringify(request),
    });
  },

  login(request: AdminLoginRequest) {
    return apiRequest<AdminAuthResponse>("/admins/login", {
      method: "POST",
      body: JSON.stringify(request),
    });
  },

  getUsers(token: string) {
    return apiRequest<AdminUserResponse[]>(
      "/admins/users",
      {},
      token
    );
  },

  getAccessRequests(token: string) {
    return apiRequest<AccessRequestResponse[]>(
      "/admins/access-requests",
      {},
      token
    );
  },

  createAccessRequest(token: string, userEmail: string) {
    return apiRequest<AccessRequestResponse>(
      "/admins/access-requests",
      {
        method: "POST",
        body: JSON.stringify({ userEmail }),
      },
      token
    );
  },

  getUserSessions(token: string, userId: number) {
    return apiRequest<SessionResponse[]>(
      `/admins/users/${userId}/sessions`,
      {},
      token
    );
  },

  getSessionFrames(
    token: string,
    userId: number,
    sessionId: number,
    page = 1
  ) {
    return apiRequest<SessionHistoryPage>(
      `/admins/users/${userId}/sessions/${sessionId}/frames?page=${page}&pageSize=10`,
      {},
      token
    );
  },


  
  
    async getFrameImage(
    token: string,
    userId: number,
    sessionId: number,
    frameId: number,
    analyzed: boolean = false,
    signal?: AbortSignal
  ): Promise<Blob> {
  
    const base =
      `/api/admins/users/${userId}` +
      `/sessions/${sessionId}` +
      `/frames/${frameId}`;
  
    const url = analyzed
      ? `${base}/analyzed-image`
      : `${base}/image`;
  
    const response = await fetch(url, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
      cache: "no-store",
      signal,
    });
  
    if (!response.ok) {
      throw new Error(
        `Image unavailable (${response.status})`
      );
    }
  
    const blob = await response.blob();
  
    if (!blob.type.startsWith("image/")) {
      throw new Error("Invalid image response.");
    }
  
    return blob;
  },
  
};
