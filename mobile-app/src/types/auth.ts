
export interface User {
  userId: number;
  name: string;
  email: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  preferredLanguage: string;
}

// Returned by BOTH register and login.
export interface AuthResponse extends User {
  token: string;
  expiresAt: string;
}
