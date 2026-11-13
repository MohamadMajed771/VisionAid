
import * as SecureStore from "expo-secure-store";
import type { AuthResponse } from "../../types/auth";

const TOKEN_KEY = "visionaid_auth_token";

export const tokenStorage = {
  // Save the authenticated user's information.
  async save(auth: AuthResponse): Promise<void> {
    await SecureStore.setItemAsync(
      TOKEN_KEY,
      JSON.stringify(auth)
    );
  },

  // Restore saved authentication on app startup.
  async get(): Promise<AuthResponse | null> {
    const stored = await SecureStore.getItemAsync(TOKEN_KEY);

    if (!stored) {
      return null;
    }

    try {
      return JSON.parse(stored) as AuthResponse;
    } catch {
      return null;
    }
  },

  // Remove saved authentication on logout.
  async remove(): Promise<void> {
    await SecureStore.deleteItemAsync(TOKEN_KEY);
  },
};
