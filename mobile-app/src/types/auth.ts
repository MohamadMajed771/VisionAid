
export type PreferredLanguage = "en" | "fr";

export interface User {
  userId: number;
  name: string;
  email: string;
  preferredLanguage: PreferredLanguage;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  preferredLanguage: PreferredLanguage;
}

// Returned by BOTH register and login.
export interface AuthResponse extends User {
  token: string;
  expiresAt: string;
}
