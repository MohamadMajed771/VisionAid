
import { Navigate, Route, Routes } from "react-router";

import GuestRoute from "./components/GuestRoute";
import ProtectedRoute from "./components/ProtectedRoute";
import DashboardLayout from "./layouts/DashboardLayout";

import Login from "./pages/Login";
import Register from "./pages/Register";
import Dashboard from "./pages/Dashboard";
import AccessRequests from "./pages/AccessRequests";
import ConnectedUsers from "./pages/ConnectedUsers";

export default function App() {
  return (
    <Routes>
      {/* Pages available only when logged out */}
      <Route element={<GuestRoute />}>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Route>

      {/* Pages available only when logged in */}
      <Route element={<ProtectedRoute />}>
        <Route element={<DashboardLayout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/requests" element={<AccessRequests />} />
          <Route path="/users" element={<ConnectedUsers />} />
        </Route>
      </Route>

      <Route
        path="*"
        element={<Navigate to="/dashboard" replace />}
      />
    </Routes>
  );
}
