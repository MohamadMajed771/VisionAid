
import { useEffect, useState, type FormEvent } from "react";
import { adminApi } from "../services/admins";
import { useAdminAuth } from "../hooks/useAdminAuth";
import type { AccessRequestResponse } from "../types/admin";
import { formatBeirutDate } from "../utils/date";

export default function AccessRequests() {
  const { admin } = useAdminAuth();

  const [email, setEmail] = useState("");
  const [requests, setRequests] = useState<AccessRequestResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  async function loadRequests() {
    if (!admin) return;

    const result = await adminApi.getAccessRequests(admin.token);
    setRequests(result);
  }

  useEffect(() => {
    let active = true;

    if (!admin) return;

    adminApi.getAccessRequests(admin.token)
      .then((data) => {
        if (active) setRequests(data);
      })
      .catch((err) => {
        if (active) setError(String(err));
      })
      .finally(() => {
        if (active) setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [admin]);

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (!admin) return;

    setError("");
    setMessage("");
    setSubmitting(true);

    try {
      await adminApi.createAccessRequest(admin.token, email.trim());
      setMessage("Monitoring request sent.");
      setEmail("");
      await loadRequests();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Request failed."
      );
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div>
      <h1>Monitoring Requests</h1>

      <section className="panel">
        <h2>Request access to a user</h2>

        <form onSubmit={handleSubmit}>
          <label htmlFor="userEmail">User email</label>
          <input
            id="userEmail"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="user@example.com"
            required
          />

          <button disabled={submitting}>
            {submitting ? "Sending..." : "Send Request"}
          </button>
        </form>

        {error && <p role="alert" className="error">{error}</p>}
        {message && <p role="status">{message}</p>}
      </section>

      <section className="panel">
        <h2>Request History</h2>

        {loading ? (
          <p>Loading...</p>
        ) : requests.length === 0 ? (
          <p>No monitoring requests yet.</p>
        ) : (
          <table>
            <thead>
              <tr>
                <th>User</th>
                <th>Email</th>
                <th>Status</th>
                <th>Requested</th>
              </tr>
            </thead>
            <tbody>
              {requests.map((request) => (
                <tr key={request.requestId}>
                  <td>{request.userName}</td>
                  <td>{request.userEmail}</td>
                  <td>{request.status}</td>
                  <td>
                   {formatBeirutDate(request.requestedAt)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
