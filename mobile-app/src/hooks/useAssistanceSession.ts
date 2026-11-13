
import { useEffect, useState } from "react";

import { sessionsApi } from "../services/api/sessions";
import { ApiError } from "../services/api/client";
import type { SessionResponse } from "../types/session";

export function useAssistanceSession(token: string | null) {
  const [session, setSession] =
    useState<SessionResponse | null>(null);

  const [loading, setLoading] = useState(true);
  const [ending, setEnding] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Start or resume a session when the screen opens.
  useEffect(() => {
    let cancelled = false;

    async function startSession() {
      if (!token) {
        setError("You must be logged in.");
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);

      try {
        let result: SessionResponse;

        try {
          // Check if this user already has an active session.
          result = await sessionsApi.getActive(token);
        } catch (err) {
          if (err instanceof ApiError && err.status === 404) {
            if (cancelled) return;

            try {
              // No active session: create a new one.
              result = await sessionsApi.create(token);
            } catch (createError) {
              if (
                createError instanceof ApiError &&
                createError.status === 409
              ) {
                // Session was created by another request.
                result = await sessionsApi.getActive(token);
              } else {
                throw createError;
              }
            }
          } else {
            throw err;
          }
        }

        if (!cancelled) {
          setSession(result);
        }
      } catch (err) {
        if (!cancelled) {
          setError(
            err instanceof Error
              ? err.message
              : "Could not start assistance."
          );
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    void startSession();

    return () => {
      cancelled = true;
    };
  }, [token]);

  // End the session when the user presses Stop.
  async function endSession(): Promise<boolean> {
    if (!session || !token || ending) {
      return false;
    }

    setEnding(true);
    setError(null);

    try {
      await sessionsApi.end(session.sessionId, token);
      setSession(null);
      return true;
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Could not end the session."
      );
      return false;
    } finally {
      setEnding(false);
    }
  }

  return {
    session,
    loading,
    ending,
    error,
    endSession,
  };
}
