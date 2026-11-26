
import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";

import { useAdminAuth } from "../hooks/useAdminAuth";
import { adminApi } from "../services/admins";
import { formatBeirutDate } from "../utils/date";
import type { SessionHistoryPage } from "../types/admin";

export default function SessionFrames() {
  const { userId, sessionId } = useParams();
  const { admin } = useAdminAuth();

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
    if (!admin || !validIds) return;

    let cancelled = false;

    async function loadFrames() {
      try {
        const result = await adminApi.getSessionFrames(
          admin!.token,
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
  }, [admin, selectedUserId, selectedSessionId, validIds, page]);

  function changePage(nextPage: number) {
    setLoading(true);
    setHistory(null);
    setPage(nextPage);
  }

  if (!validIds) {
    return <p>Invalid user or session ID.</p>;
  }

  return (
    <section>
      <Link to={`/users/${selectedUserId}/sessions`}>
        Back to sessions
      </Link>

      <h1>Session #{selectedSessionId}</h1>

      {loading && <p>Loading frame history...</p>}
      {error && <p role="alert">{error}</p>}

      {!loading && !error && history && (
        <>
          <p>Total original frames: {history.totalFrames}</p>

          {history.frames.length === 0 && (
            <p>No frames have been recorded.</p>
          )}

          {history.frames.map((frame, index) => (
            <article
              key={`${frame.frameId}-${frame.analyzedFrameId ?? index}`}
            >
              <h3>Frame #{frame.frameId}</h3>

              <p>
                Captured: {formatBeirutDate(frame.capturedAt)}
              </p>

              <p>
                Original image: {frame.originalImageName}
              </p>

              {frame.analyzedFrameId === null ? (
                <p>Analysis not available.</p>
              ) : (
                <>
                  <p>
                    Analyzed: {formatBeirutDate(frame.analyzedAt)}
                  </p>

                  <p>
                    Analyzed image: {frame.analyzedImagePath}
                  </p>

                  <p>
                    Question: {frame.question || "None"}
                  </p>

                  <p>
                    Answer: {frame.answer || "None"}
                  </p>

                  <p>
                    Dimensions: {frame.imageWidth ?? "?"}
                    {" × "}
                    {frame.imageHeight ?? "?"}
                  </p>
                </>
              )}
            </article>
          ))}

          <div>
            <button
              disabled={loading || page <= 1}
              onClick={() => changePage(page - 1)}
            >
              Previous
            </button>

            <span>
              {" "} Page {history.page} of{" "}
              {Math.max(1, history.totalPages)} {" "}
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
        </>
      )}
    </section>
  );
}
