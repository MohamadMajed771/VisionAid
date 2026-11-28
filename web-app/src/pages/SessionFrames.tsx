
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";

import { useAdminAuth } from "../hooks/useAdminAuth";
import { adminApi } from "../services/admins";
import { formatBeirutDate } from "../utils/date";
import type { SessionHistoryPage } from "../types/admin";

import ProtectedFrameImage
  from "../components/ProtectedFrameImage";

export default function SessionFrames() {
  const { userId, sessionId } = useParams();
  const { admin } = useAdminAuth();

  const token = admin?.token;

  const selectedUserId = Number(userId);
  const selectedSessionId = Number(sessionId);

  const [page, setPage] = useState(1);

  const [history, setHistory] =
    useState<SessionHistoryPage | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const validIds =
    Number.isInteger(selectedUserId) &&
    selectedUserId > 0 &&
    Number.isInteger(selectedSessionId) &&
    selectedSessionId > 0;

  useEffect(() => {
    if (!token || !validIds) return;

    const activeToken = token;
    let cancelled = false;

    async function loadFrames() {
      try {
        const result = await adminApi.getSessionFrames(
          activeToken,
          selectedUserId,
          selectedSessionId,
          page
        );

        if (!cancelled) {
          setHistory(result);
          setError("");
        }
      } catch (err) {
        if (!cancelled) {
          setHistory(null);

          setError(
            err instanceof Error
              ? err.message
              : "Could not load frame history."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void loadFrames();

    return () => {
      cancelled = true;
    };
  }, [
    token,
    selectedUserId,
    selectedSessionId,
    validIds,
    page,
  ]);

  function changePage(nextPage: number) {
    if (
      !history ||
      nextPage < 1 ||
      nextPage > history.totalPages ||
      nextPage === page
    ) {
      return;
    }

    setLoading(true);
    setError("");
    setHistory(null);
    setPage(nextPage);
  }

  if (!token) {
    return <p>You must be logged in.</p>;
  }

  if (!validIds) {
    return <p>Invalid user or session ID.</p>;
  }

  return (
    <section className="monitor-page">
      <Link
        className="monitor-back"
        to={`/users/${selectedUserId}/sessions`}
      >
        ← Back to sessions
      </Link>

      <h1>Session #{selectedSessionId}</h1>

      {loading && <p>Loading frame history...</p>}

      {error && (
        <p role="alert">{error}</p>
      )}

      {!loading && !error && history && (
        <>
          <p>
            Total original frames: {history.totalFrames}
          </p>

          {history.frames.length === 0 && (
            <p>No frames have been recorded.</p>
          )}

          {history.frames.map((frame) => (
            <article
                className="monitor-card"
                key={frame.frameId}
            >
              <h3>Frame #{frame.frameId}</h3>

              <p>
                Captured: {formatBeirutDate(frame.capturedAt)}
              </p>

                          
            <div className="monitor-image-grid">
              <div>
                <h4>Original image</h4>
                    
                <ProtectedFrameImage
                  token={token}
                  userId={selectedUserId}
                  sessionId={selectedSessionId}
                  frameId={frame.frameId}
                  alt={`Original frame ${frame.frameId}`}
                />
              </div>
                    
              {frame.analyzedFrameId != null && (
                <div>
                  <h4>Analyzed image</h4>
            
                  <ProtectedFrameImage
                    token={token}
                    userId={selectedUserId}
                    sessionId={selectedSessionId}
                    frameId={frame.frameId}
                    analyzed={true}
                    alt={`Analyzed frame ${frame.frameId}`}
                  />
                </div>
              )}
            </div>


              {frame.analyzedFrameId == null ? (
                <p>Analysis not available.</p>
              ) : (
                <>
                  <p>
                    Analyzed: {formatBeirutDate(frame.analyzedAt)}
                  </p>

                  <div className="monitor-question">
                    <strong>Question</strong>

                    <p>
                      {frame.question || "No question asked."}
                    </p>
                  </div>

                  <div className="monitor-answer">
                    <strong>AI Answer</strong>

                    <p>
                      {frame.answer || "No answer available."}
                    </p>
                  </div>

                  <p>
                    Dimensions: {frame.imageWidth ?? "?"}
                    {" × "}
                    {frame.imageHeight ?? "?"}
                  </p>
                </>
              )}
            </article>
          ))}

          {history.totalPages > 1 && (
            <div className="monitor-pagination">
              <button
                disabled={loading || page <= 1}
                onClick={() => changePage(page - 1)}
              >
                Previous
              </button>

              <span>
                Page {history.page} of {history.totalPages}
              </span>

              <button
                disabled={
                  loading ||
                  page >= history.totalPages
                }
                onClick={() => changePage(page + 1)}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
