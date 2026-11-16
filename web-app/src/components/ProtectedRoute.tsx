
import { Navigate, Outlet } from "react-router";
import { useAdminAuth } from "../hooks/useAdminAuth";

export default function ProtectedRoute() {
  const { admin } = useAdminAuth();

  if (!admin) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
