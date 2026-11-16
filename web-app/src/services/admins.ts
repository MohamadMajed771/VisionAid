
import { apiRequest } from "./api";
import type {
  AdminAuthResponse,
  AdminLoginRequest,
  AdminRegisterRequest,
  AdminUserResponse,
  AccessRequestResponse,
  SessionResponse,
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
};
