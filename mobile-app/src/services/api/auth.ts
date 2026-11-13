
import { apiRequest } from "./client";

import type {
  AuthResponse,
  RegisterRequest,
  LoginRequest,
} from "../../types/auth";

const AUTH_ENDPOINTS = {
  register: "/api/auth/register",
  login: "/api/auth/login",
};

export const authApi = {
  register(data: RegisterRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>(
      AUTH_ENDPOINTS.register,
      {
        method: "POST",
        body: JSON.stringify(data),
      }
    );
  },

  login(data: LoginRequest): Promise<AuthResponse> {
    return apiRequest<AuthResponse>(
      AUTH_ENDPOINTS.login,
      {
        method: "POST",
        body: JSON.stringify(data),
      }
    );
  },
};
