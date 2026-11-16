
import { createContext } from "react";
import type {
  AdminAuthResponse,
  AdminLoginRequest,
  AdminRegisterRequest,
} from "../types/admin";

export interface AdminAuthValue {
  admin: AdminAuthResponse | null;
  login: (request: AdminLoginRequest) => Promise<void>;
  register: (request: AdminRegisterRequest) => Promise<void>;
  logout: () => void;
}

export const AdminAuthContext =
  createContext<AdminAuthValue | null>(null);
