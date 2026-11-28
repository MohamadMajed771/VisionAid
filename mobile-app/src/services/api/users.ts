
import { apiRequest } from "./client";

import type {
  User,
  PreferredLanguage,
} from "../../types/auth";

interface UserProfile extends User {
  createdAt: string;
}

interface LanguageResponse {
  message: string;
  preferredLanguage: PreferredLanguage;
}

export const usersApi = {
  getProfile(token: string, userId: number) {
    return apiRequest<UserProfile>(
      `/api/users/${userId}`,
      {},
      token
    );
  },

  updatePreferredLanguage(
    token: string,
    language: PreferredLanguage
  ) {
    return apiRequest<LanguageResponse>(
      "/api/users/preferred-language",
      {
        method: "PUT",
        body: JSON.stringify({ language }),
      },
      token
    );
  },
};
