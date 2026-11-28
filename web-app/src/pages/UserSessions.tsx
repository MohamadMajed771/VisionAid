
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";

import { useAdminAuth } from "../hooks/useAdminAuth";
import { adminApi } from "../services/admins";
import { formatBeirutDate } from "../utils/date";
import type { SessionResponse } from "../types/admin";

export default function UserSessions() {
  const { userId } = useParams(); //use the url of the paage to egt the userID 
  const { admin } = useAdminAuth();

  const selectedUserId = Number(userId);

  const [sessions, setSessions] = useState<SessionResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!admin || !Number.isInteger(selectedUserId) ||
        selectedUserId <= 0) {
      return;
    }

    let cancelled = false;

    async function loadSessions() {
      try {
        const result = await adminApi.getUserSessions(
          admin!.token,
          selectedUserId
        );

        if (!cancelled) {
          setSessions(result);
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not load sessions."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadSessions();

    return () => {
      cancelled = true;
    };
  }, [admin, selectedUserId]);

  if (!Number.isInteger(selectedUserId) ||
      selectedUserId <= 0) {
    return <p>Invalid user ID.</p>;
  }

  return (
    <section className="monitor-page">
        
     <Link className="monitor-back" to="/users">
      ← Back to connected users
    </Link>

      <h1>User Sessions</h1>

      {loading && <p>Loading sessions...</p>}
      {error && <p role="alert">{error}</p>}

      {!loading && !error && (
        <>
          <p>Total sessions: {sessions.length}</p>

          {sessions.length === 0 && (
            <p>This user has no sessions yet.</p>
          )}

          {sessions.map((session) => (
            <article key={session.sessionId}  className="monitor-card">   
              <h3>Session #{session.sessionId}</h3>

              <p>
                <span
                  className={`monitor-status ${
                    session.status.toLowerCase()
                  }`}
                >
                  {session.status}
                </span>
             </p>

              <p>
                Started: {formatBeirutDate(session.startedAt)}
              </p>

              <p>
                Ended: {formatBeirutDate(session.endedAt)}
              </p>

              <Link  className="monitor-link"
                to={`/users/${selectedUserId}/sessions/${session.sessionId}`}
              >
                View frame history
              </Link>
            </article>
          ))}
        </>
      )}
    </section>
  );
}
