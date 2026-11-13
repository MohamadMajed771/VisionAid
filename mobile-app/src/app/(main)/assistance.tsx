
import {
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";

import {
  AppState,
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from "react-native";

import { CameraView } from "expo-camera";
import { router, useFocusEffect } from "expo-router";

import CameraPreview from "../../components/assistance/CameraPreview";
import { useAuth } from "../../contexts/AuthContext";
import { useAssistanceSession } from "../../hooks/useAssistanceSession";
import { useAutomaticFrameCapture } from "../../hooks/useAutomaticFrameCapture";

export default function AssistanceScreen() {
  const { token } = useAuth();

  const cameraRef = useRef<CameraView>(null);

  const [cameraReady, setCameraReady] = useState(false);
  const [cameraError, setCameraError] =
    useState<string | null>(null);

  const [focused, setFocused] = useState(false);

  const [appActive, setAppActive] = useState(
    AppState.currentState === "active"
  );

  const [stopping, setStopping] = useState(false);

  // Track when the Assistance screen is visible.
  useFocusEffect(
    useCallback(() => {
      setFocused(true);

      return () => {
        setFocused(false);
        setCameraReady(false);
      };
    }, [])
  );

  // Pause the camera when the app enters background.
  useEffect(() => {
    const subscription = AppState.addEventListener(
      "change",
      state => {
        const active = state === "active";
        setAppActive(active);

        if (!active) {
          setCameraReady(false);
        }
      }
    );

    return () => subscription.remove();
  }, []);

  const {
    session,
    loading,
    ending,
    error: sessionError,
    endSession,
  } = useAssistanceSession(token);

  const cameraVisible = focused && appActive;

  const handleCameraReady = useCallback(() => {
    setCameraError(null);
    setCameraReady(true);
  }, []);

  const handleCameraError = useCallback(
    (message: string) => {
      setCameraReady(false);
      setCameraError(message);
    },
    []
  );

  // Take one photo and return its local URI.
  const captureFrame = useCallback(async (): Promise<string> => {
    if (!cameraRef.current) {
      throw new Error("Camera is not ready.");
    }

    const photo = await cameraRef.current.takePictureAsync({
      quality: 0.5,
      base64: false,
      skipProcessing: false,
    });

    if (!photo?.uri) {
      throw new Error("Could not capture a frame.");
    }

    return photo.uri;
  }, []);

  const {
    frameCount,
    uploading,
    lastAnswer,
    error: frameError,
    pause,
    retry,
    waitForCurrentUpload,
  } = useAutomaticFrameCapture({
    sessionId: session?.sessionId ?? null,
    token,
    enabled:
      cameraVisible &&
      cameraReady &&
      !!session &&
      !stopping &&
      !ending,
    captureFrame,
  });

  async function handleStop() {
    if (stopping || !session) {
      return;
    }

    // Stop scheduling new frames immediately.
    pause();
    setStopping(true);

    // Let a currently running analysis finish.
    await waitForCurrentUpload();

    const ended = await endSession();

    if (ended) {
      router.replace("/home");
    } else {
      setStopping(false);
    }
  }

  return (
    <ScrollView
      contentContainerStyle={styles.container}
    >
      <Text style={styles.title}>
        VisionAid Assistance
      </Text>

      {loading && (
        <View style={styles.center}>
          <ActivityIndicator size="large" />
          <Text>Starting session...</Text>
        </View>
      )}

      {session && (
        <>
          <Text style={styles.status}>
            Session #{session.sessionId} — {session.status}
          </Text>

          <View style={styles.cameraContainer}>
            {cameraVisible ? (
              <CameraPreview
                cameraRef={cameraRef}
                onReady={handleCameraReady}
                onError={handleCameraError}
              />
            ) : (
              <View style={styles.center}>
                <Text>Camera paused</Text>
              </View>
            )}
          </View>

          <Text style={styles.info}>
            {stopping
              ? "Finishing the current analysis..."
              : uploading
                ? "Analyzing camera frame..."
                : cameraReady && cameraVisible
                  ? "Automatic analysis is ready"
                  : "Waiting for camera"}
          </Text>

          <Text style={styles.info}>
            Frames analyzed: {frameCount}
          </Text>

          {lastAnswer && (
            <View style={styles.answerContainer}>
              <Text style={styles.answerTitle}>
                Latest AI Answer
              </Text>
              <Text style={styles.answer}>
                {lastAnswer}
              </Text>
            </View>
          )}

          {cameraError && (
            <Text style={styles.error}>{cameraError}</Text>
          )}

          {frameError && (
            <View style={styles.center}>
              <Text style={styles.error}>{frameError}</Text>

              <Pressable
                style={styles.retryButton}
                onPress={retry}
                accessibilityRole="button"
              >
                <Text style={styles.buttonText}>
                  Retry Analysis
                </Text>
              </Pressable>
            </View>
          )}

          <Pressable
            style={styles.stopButton}
            onPress={() => void handleStop()}
            disabled={stopping || ending}
            accessibilityRole="button"
            accessibilityLabel="End assistance session"
          >
            <Text style={styles.buttonText}>
              {stopping || ending
                ? "Ending..."
                : "End Session"}
            </Text>
          </Pressable>
        </>
      )}

      {sessionError && (
        <Text style={styles.error}>
          {sessionError}
        </Text>
      )}

      {!loading && !session && (
        <Pressable
          style={styles.retryButton}
          onPress={() => router.replace("/home")}
          accessibilityRole="button"
        >
          <Text style={styles.buttonText}>
            Back to Home
          </Text>
        </Pressable>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    backgroundColor: "#F4F7FB",
    padding: 20,
    gap: 18,
  },
  title: {
    fontSize: 28,
    fontWeight: "bold",
    textAlign: "center",
    color: "#17375E",
  },
  status: {
    fontSize: 18,
    textAlign: "center",
    color: "#067647",
  },
  cameraContainer: {
    height: 340,
    borderRadius: 16,
    overflow: "hidden",
    backgroundColor: "#172033",
  },
  center: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 15,
    gap: 10,
  },
  info: {
    fontSize: 17,
    textAlign: "center",
  },
  answerContainer: {
    padding: 18,
    borderRadius: 12,
    backgroundColor: "#EAF2FF",
    gap: 8,
  },
  answerTitle: {
    fontSize: 18,
    fontWeight: "bold",
  },
  answer: {
    fontSize: 17,
  },
  error: {
    color: "#B42318",
    fontSize: 16,
    textAlign: "center",
  },
  stopButton: {
    backgroundColor: "#B42318",
    padding: 20,
    borderRadius: 12,
    alignItems: "center",
  },
  retryButton: {
    backgroundColor: "#175CD3",
    padding: 16,
    borderRadius: 12,
    alignItems: "center",
  },
  buttonText: {
    color: "#FFFFFF",
    fontSize: 19,
    fontWeight: "bold",
  },
});
