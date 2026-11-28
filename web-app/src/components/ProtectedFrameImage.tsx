
import { useEffect, useState } from "react";
import { adminApi } from "../services/admins";

type Props = {
  token: string;
  userId: number;
  sessionId: number;
  frameId: number;
  analyzed?: boolean;
  alt: string;
};

type ImageResult = {
  key: string;
  url: string | null;
  error: string;
};

export default function ProtectedFrameImage({
  token,
  userId,
  sessionId,
  frameId,
  analyzed = false,
  alt,
}: Props) {

  const key =
    `${userId}-${sessionId}-${frameId}-${analyzed}`;

  const [result, setResult] =
    useState<ImageResult | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    let cancelled = false;
    let objectUrl: string | null = null;

    async function loadImage() {
      try {
        const blob = await adminApi.getFrameImage(
          token,
          userId,
          sessionId,
          frameId,
          analyzed,
          controller.signal
        );

        if (cancelled) return;

        objectUrl = URL.createObjectURL(blob);

        setResult({
          key,
          url: objectUrl,
          error: "",
        });

      } catch (err) {
        if (cancelled) return;

        setResult({
          key,
          url: null,
          error:
            err instanceof Error
              ? err.message
              : "Could not load image.",
        });
      }
    }

    void loadImage();

    return () => {
      cancelled = true;

      controller.abort();

      if (objectUrl) {
        URL.revokeObjectURL(objectUrl);
      }
    };

  }, [
    token,
    userId,
    sessionId,
    frameId,
    analyzed,
    key,
  ]);

  if (!result || result.key !== key) {
    return <p>Loading image...</p>;
  }

  if (result.error) {
    return <p role="alert">{result.error}</p>;
  }

  if (!result.url) return null;

  return (
    <img
      src={result.url}
      alt={alt}
      className="monitor-frame-image"
    />
  );
}
