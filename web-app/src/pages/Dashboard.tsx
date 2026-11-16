
import { Link } from "react-router";
import { useAdminAuth } from "../hooks/useAdminAuth";

export default function Dashboard() {
  const { admin } = useAdminAuth();

  return (
    <div>
      <h1>Welcome, {admin?.name}</h1>
      <p>Manage the users who have granted you monitoring access.</p>

      <div className="cards">
        <section className="panel">
          <h2>Access Requests</h2>
          <p>Request monitoring permission using a user's email.</p>
          <Link to="/requests">Manage requests</Link>
        </section>

        <section className="panel">
          <h2>Connected Users</h2>
          <p>View accepted users and their sessions.</p>
          <Link to="/users">View users</Link>
        </section>
      </div>
    </div>
  );
}
