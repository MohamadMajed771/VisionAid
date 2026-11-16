
export interface AdminAuthResponse {
  adminId: number;
  name: string;
  email: string;
  token: string;
  expiresAt: string;
}

export interface AdminLoginRequest {
  email: string;
  password: string;
}

export interface AdminRegisterRequest extends AdminLoginRequest {
  name: string;
}

export interface SessionResponse {
  sessionId: number;
  userId: number;
  startedAt: string;
  endedAt: string | null;
  status: string;
}

export interface AdminUserResponse {
  userId: number;
  name: string;
  email: string;
  sessions: SessionResponse[];
}

export interface AccessRequestResponse {
  requestId: number;
  adminId: number;
  adminName: string;
  adminEmail: string;
  userId: number;
  userName: string;
  userEmail: string;
  status: string;
  requestedAt: string;
  respondedAt: string | null;
}
