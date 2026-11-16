
import { Navigate, Outlet } from "react-router";
import { useAdminAuth } from "../hooks/useAdminAuth";

export default function GuestRoute() {
  const { admin } = useAdminAuth();

  if (admin) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
