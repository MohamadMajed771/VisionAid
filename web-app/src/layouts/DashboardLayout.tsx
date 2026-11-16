
import { NavLink, Outlet, useNavigate } from "react-router";
import { useAdminAuth } from "../hooks/useAdminAuth";

export default function DashboardLayout() {
  const { admin, logout } = useAdminAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  return (
    <>
      <header className="topbar">
        <strong>VisionAid Monitoring</strong>

        <nav>
          <NavLink to="/dashboard">Dashboard</NavLink>
          <NavLink to="/requests">Access Requests</NavLink>
          <NavLink to="/users">Connected Users</NavLink>
        </nav>

        <span>{admin?.name}</span>
        <button onClick={handleLogout}>Logout</button>
      </header>

      <main className="page">
        <Outlet />
      </main>
    </>
  );
}
