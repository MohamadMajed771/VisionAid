
import { useState, type ReactNode } from "react";
import { adminApi } from "../services/admins";
import type {
  AdminAuthResponse,
  AdminLoginRequest,
  AdminRegisterRequest,
} from "../types/admin";
import { AdminAuthContext } from "./AdminAuthContext";

const STORAGE_KEY = "visionaid_monitor_session";

function getStoredAdmin(): AdminAuthResponse | null {
  try {
    const saved = sessionStorage.getItem(STORAGE_KEY);
    if (!saved) return null;

    const admin = JSON.parse(saved) as AdminAuthResponse;

    if (
      !admin.token ||
      Date.parse(admin.expiresAt) <= Date.now()
    ) {
      sessionStorage.removeItem(STORAGE_KEY);
      return null;
    }

    return admin;
  } catch {
    sessionStorage.removeItem(STORAGE_KEY);
    return null;
  }
}

export function AdminAuthProvider({
  children,
}: {
  children: ReactNode;
}) {
  const [admin, setAdmin] = useState(getStoredAdmin);

  function saveAdmin(response: AdminAuthResponse) {
    sessionStorage.setItem(
      STORAGE_KEY,
      JSON.stringify(response)
    );
    setAdmin(response);
  }

  async function login(request: AdminLoginRequest) {
    const response = await adminApi.login(request);
    saveAdmin(response);
  }

  async function register(request: AdminRegisterRequest) {
    const response = await adminApi.register(request);
    saveAdmin(response);
  }

  function logout() {
    sessionStorage.removeItem(STORAGE_KEY);
    setAdmin(null);
  }

  return (
    <AdminAuthContext.Provider
      value={{ admin, login, register, logout }}
    >
      {children}
    </AdminAuthContext.Provider>
  );
}
