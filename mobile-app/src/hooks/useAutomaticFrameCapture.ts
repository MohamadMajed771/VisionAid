
import {
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";

import { framesApi } from "../services/api/frames";

interface Options {
  sessionId: number | null;
  token: string | null;
  enabled: boolean;
  captureFrame: () => Promise<string>;
}

const CAPTURE_DELAY_MS = 3000;

export function useAutomaticFrameCapture({
  sessionId,
  token,
  enabled,
  captureFrame,
}: Options) {
  const [frameCount, setFrameCount] = useState(0);
  const [uploading, setUploading] = useState(false);
  const [lastAnswer, setLastAnswer] =
    useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [retryKey, setRetryKey] = useState(0);

  const pausedRef = useRef(false);

  const timerRef =
    useRef<ReturnType<typeof setTimeout> | null>(null);

  const inFlightRef = useRef<Promise<void> | null>(null);

  // Immediately prevent scheduling additional frames.
  const pause = useCallback(() => {
    pausedRef.current = true;

    if (timerRef.current !== null) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  // Wait for an already-running capture/upload to finish.
  const waitForCurrentUpload = useCallback(async () => {
    if (inFlightRef.current) {
      await inFlightRef.current;
    }
  }, []);

  const retry = useCallback(() => {
    setError(null);
    setRetryKey(value => value + 1);
  }, []);

  useEffect(() => {
    if (!enabled || !sessionId || !token) {
      return;
    }
     const activeSessionId: number = sessionId;
    const activeToken: string = token;
    let cancelled = false;
    pausedRef.current = false;

    async function captureAndUpload() {
      if (cancelled || pausedRef.current) {
        return;
      }

      // Do not start while a previous upload is running.
      if (inFlightRef.current) {
        await inFlightRef.current;
      }

      if (cancelled || pausedRef.current) {
        return;
      }

      const work = (async () => {
        setUploading(true);

        try {
          // 1. Take a photo from the camera.
          const imageUri = await captureFrame();

          if (cancelled || pausedRef.current) {
            return;
          }

          // 2. Send it to the backend.
          const result = await framesApi.upload(
            activeSessionId,
            imageUri,
            activeToken 
          );

          if (!cancelled && !pausedRef.current) {
            setFrameCount(count => count + 1);
            setLastAnswer(result.answer);
            setError(null);
          }
        } catch (err) {
          if (!cancelled && !pausedRef.current) {
            setError(
              err instanceof Error
                ? err.message
                : "Frame analysis failed."
            );

            // Pause instead of repeatedly uploading
            // when the backend or AI service fails.
            pausedRef.current = true;
          }
        } finally {
          if (!cancelled) {
            setUploading(false);
          }
        }
      })();

      inFlightRef.current = work;

      await work;

      if (inFlightRef.current === work) {
        inFlightRef.current = null;
      }

      // 3. Schedule the next capture AFTER analysis.
      if (!cancelled && !pausedRef.current) {
        timerRef.current = setTimeout(() => {
          void captureAndUpload();
        }, CAPTURE_DELAY_MS);
      }
    }

    void captureAndUpload();

    return () => {
      cancelled = true;
      pausedRef.current = true;

      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
        timerRef.current = null;
      }
    };
  }, [enabled, sessionId, token, captureFrame, retryKey]);

  return {
    frameCount,
    uploading,
    lastAnswer,
    error,
    pause,
    retry,
    waitForCurrentUpload,
  };
}
