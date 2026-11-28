
import { apiRequest } from "./client";

export interface AccessRequest {
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

export interface AcceptedMonitor {
  adminId: number;
  name: string;
  email: string;
}
export const accessRequestsApi = {

  getAcceptedMonitors(token: string) {
    return apiRequest<AcceptedMonitor[]>(
      "/api/users/access-requests/admins",
      {},
      token
    );
  },
  getPending(token: string) {
    return apiRequest<AccessRequest[]>(
      "/api/users/access-requests",
      {},
      token
    );
  },

  accept(token: string, requestId: number) {
    return apiRequest<void>(
      `/api/users/access-requests/${requestId}/accept`,
      { method: "POST" },
      token
    );
  },

  decline(token: string, requestId: number) {
    return apiRequest<void>(
      `/api/users/access-requests/${requestId}/decline`,
      { method: "POST" },
      token
    );
  },

  revoke(token: string, adminId: number) {
    return apiRequest<void>(
      `/api/users/access-requests/admins/${adminId}`,
      { method: "DELETE" },
      token
    );
  },
};
