
import { useEffect, useState } from "react";
import { adminApi } from "../services/admins";
import { useAdminAuth } from "../hooks/useAdminAuth";
import type { AdminUserResponse } from "../types/admin";

import { Link } from "react-router";

export default function ConnectedUsers() {
  const { admin } = useAdminAuth();

  const [users, setUsers] = useState<AdminUserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;

    if (!admin) return;

    adminApi.getUsers(admin.token)
      .then((result) => {
        if (active) setUsers(result);
      })
      .catch((err) => {
        if (active) {
          setError(
            err instanceof Error ? err.message : "Failed to load users."
          );
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [admin]);

  if (loading) return <p>Loading connected users...</p>;
  if (error) return <p role="alert" className="error">{error}</p>;

  return (
    <div>
      <h1>Connected Users</h1>

      {users.length === 0 ? (
        <p>
          No users have granted you monitoring access yet.
        </p>
      ) : (
        users.map((user) => (
          <section key={user.userId} className="panel">
            <h2>{user.name}</h2>
            <p>{user.email}</p>
           
           <Link to={`/users/${user.userId}/sessions`}>
            View sessions
          </Link>
          </section>
        ))
      )}
    </div>
  );
}
