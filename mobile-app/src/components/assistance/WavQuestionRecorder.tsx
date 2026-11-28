
import { useEffect, useRef, useState } from "react";

import {
  Pressable,
  StyleSheet,
  Text,
  View,
} from "react-native";

import { useAudioStream } from "expo-audio";

type Props = {
  disabled: boolean;
  onRecorded: (audioUri: string) => Promise<void>;
  onActivityChange: (active: boolean) => Promise<void>;
};

export default function WavQuestionRecorder({
  disabled,
  onRecorded,
  onActivityChange,
}: Props) {
  const { stream } = useAudioStream({
    sampleRate: 16000,
    channels: 1,
    encoding: "int16",
  });

  const [recording, setRecording] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const busyRef = useRef(false);
  const mountedRef = useRef(true);


  const recordingRef = useRef(false);
  const holdActiveRef = useRef(false);
  const holdReleasedRef = useRef(false);
  useEffect(() => {
    mountedRef.current = true;

    return () => {
      console.log("[Recorder] Cleanup executed");
      mountedRef.current = false;
      stream.stop();
    };
  }, [stream]);

  
  async function startRecording() {


      if (
      disabled ||
      recordingRef.current ||
      busyRef.current
    ) {
      return;
    }

    console.log("[Recorder] Starting");

    busyRef.current = true;

    setBusy(true);
    setError(null);

    try {
      // Permission was already checked in Home.

      console.log("[Recorder] Pausing automatic analysis");

      await onActivityChange(true);

      console.log("[Recorder] Automatic analysis paused");

      if (!mountedRef.current) return;

      console.log("[Recorder] Starting audio stream");

      await stream.start();

      console.log("[Recorder] Audio stream started");

      if (!mountedRef.current) {
        stream.stop();
        return;
      }

      await stream.startFileRecordingAsync({
        format: "wav",
      });

      console.log("[Recorder] WAV recording started");

      if (!mountedRef.current) {
        stream.stop();
        return;
      }

      recordingRef.current = true;
      setRecording(true);

    } catch (err) {
      console.error("[Recorder] Start failed:", err);

      recordingRef.current = false;
      stream.stop();

      if (mountedRef.current) {
        setError(
          err instanceof Error
            ? err.message
            : "Could not start recording."
        );

        await onActivityChange(false);
      }

    } finally {
      busyRef.current = false;

      if (mountedRef.current) {
        setBusy(false);
      }
    }
  }


  async function stopRecording() {
    if (
      !recordingRef.current ||
      busyRef.current
    ) {
      return;
    }

    busyRef.current = true;
    setBusy(true);

    try {
      const result =
        await stream.stopFileRecordingAsync();

      stream.stop();

      recordingRef.current = false;

      if (!mountedRef.current) return;

      setRecording(false);

      await onRecorded(result.uri);

    } catch (err) {
      stream.stop();

      recordingRef.current = false;

      if (mountedRef.current) {
        setRecording(false);

        setError(
          err instanceof Error
            ? err.message
            : "Could not process question."
        );
      }

    } finally {
      busyRef.current = false;
      holdActiveRef.current = false;
      holdReleasedRef.current = false;

      if (mountedRef.current) {
        await onActivityChange(false);
        setBusy(false);
      }
    }
  }

  async function handlePressIn() {
    if (
      disabled ||
      busyRef.current ||
      recordingRef.current ||
      holdActiveRef.current
    ) {
      return;
    }

    holdActiveRef.current = true;
    holdReleasedRef.current = false;

    await startRecording();

    if (
      !mountedRef.current ||
      !recordingRef.current
    ) {
      holdActiveRef.current = false;
      return;
    }

    // The finger may already have been released
    // while recording was initializing.
    if (holdReleasedRef.current) {
      await stopRecording();
    }
  }

  function handlePressOut() {
    if (!holdActiveRef.current) return;

    holdReleasedRef.current = true;

    if (
      recordingRef.current &&
      !busyRef.current
    ) {
      void stopRecording();
    }
  }


  return (
    <View style={styles.container}>

      {/* Normal recording button */}
      <Pressable
        style={[
          styles.button,
          recording &&
            !holdActiveRef.current &&
            styles.recordingButton,
          (busy || (disabled && !recording)) &&
            styles.disabled,
        ]}
        disabled={
          busy ||
          holdActiveRef.current ||
          (disabled && !recording)
        }
        onPress={() => {
          if (holdActiveRef.current) return;

          void (
            recording
              ? stopRecording()
              : startRecording()
          );
        }}
        accessibilityRole="button"
        accessibilityLabel={
          recording
            ? "Stop and send question"
            : "Ask a question"
        }
      >
        <Text style={styles.buttonText}>
          {busy
            ? "Please wait..."
            : recording && !holdActiveRef.current
              ? "Stop and Send Question"
              : "Ask Question"}
        </Text>
      </Pressable>

      {/* Hold-to-talk recording area */}
      <Pressable
        style={[
          styles.holdArea,
          recording &&
            holdActiveRef.current &&
            styles.recordingButton,
        ]}
        disabled={
          !holdActiveRef.current &&
          (disabled || busy || recording)
        }
        onPressIn={() => void handlePressIn()}
        onPressOut={handlePressOut}
        accessibilityRole="button"
        accessibilityLabel="Hold to ask a question"
      >
        <Text style={styles.buttonText}>
          {recording && holdActiveRef.current
            ? "Release to Send"
            : busy && holdActiveRef.current
              ? "Please wait..."
              : "Hold to Ask"}
        </Text>
      </Pressable>

      {/* Recording information */}
      {recording && (
        <Text style={styles.info}>
          Recording your question...
        </Text>
      )}

      {/* Error message */}
      {error && (
        <Text style={styles.error}>
          {error}
        </Text>
      )}

    </View>
  );

}

const styles = StyleSheet.create({
  container: {
    gap: 10,
  },
  button: {
    backgroundColor: "#175CD3",
    padding: 22,
    borderRadius: 14,
    alignItems: "center",
  },
  recordingButton: {
    backgroundColor: "#B42318",
  },
  disabled: {
    opacity: 0.5,
  },
  buttonText: {
    color: "#FFFFFF",
    fontWeight: "bold",
    fontSize: 20,
    textAlign: "center",
  },
  info: {
    color: "#475569",
    fontSize: 17,
    textAlign: "center",
  },
  error: {
    color: "#B42318",
    textAlign: "center",
  },

    holdArea: {
    backgroundColor: "#0F766E",
    minHeight: 150,
    borderRadius: 16,
    justifyContent: "center",
    alignItems: "center",
    padding: 20,
  },
});
