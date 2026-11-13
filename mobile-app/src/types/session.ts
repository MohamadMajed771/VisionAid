
export interface SessionResponse {
  sessionId: number;
  userId: number;
  startedAt: string;
  endedAt: string | null;
  status: string;
}
