import { useCallback, useEffect, useRef, useState } from "react";
import WavQuestionRecorder from "../../components/assistance/WavQuestionRecorder";
import { framesApi } from "../../services/api/frames";
import { speakAnswer, stopSpeaking } from "../../services/audio/tts";

import {
  ActivityIndicator,
  AppState,
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
  const { user, token } = useAuth();

  const preferredLanguage = user?.preferredLanguage ?? "en";

  const cameraRef = useRef<CameraView>(null);

  const [cameraReady, setCameraReady] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);

  const [focused, setFocused] = useState(false);
  // focused make sure the assistance screen is active as the navigation screen
  const [appActive, setAppActive] = useState(
    AppState.currentState === "active",
  );
  // make sure we are opening the applicationto capture images (and not another app while our in background)
  const [stopping, setStopping] = useState(false);
  const lastSpokenAnswerRef = useRef<string | null>(null);

  const [questionActive, setQuestionActive] = useState(false);

  const [questionSending, setQuestionSending] = useState(false);

  const [latestAnswer, setLatestAnswer] = useState<string | null>(null);

  const questionActiveRef = useRef(false);
  const questionSendingRef = useRef(false);

  const focusedRef = useRef(false);
  const visibleRef = useRef(false);
  const visibilityVersion = useRef(0);

  const lastAutomaticFrameRef = useRef<number | null>(null);
  // Track when the Assistance screen is visible.
  useFocusEffect(
    useCallback(() => {
      focusedRef.current = true;
      visibleRef.current = AppState.currentState === "active";

      setFocused(true);

      return () => {
        focusedRef.current = false;
        visibleRef.current = false;
        visibilityVersion.current++;

        questionActiveRef.current = false;
        setQuestionActive(false);

        setFocused(false);
        setCameraReady(false);

        void stopSpeaking();
      };
    }, []),
  );

  // Pause the camera when the app enters background.
  useEffect(() => {
    const subscription = AppState.addEventListener("change", (state) => {
      console.log("[Assistance] AppState:", state);
      const active = state === "active";

      setAppActive(active);

      visibleRef.current = active && focusedRef.current;

      if (!active) {
        visibilityVersion.current++;

        questionActiveRef.current = false;
        setQuestionActive(false);

        setCameraReady(false);

        void stopSpeaking();
      }
    });

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
  // to make sure i am opening assistance.tsx and the app at the same time
  const handleCameraReady = useCallback(() => {
    setCameraError(null);
    setCameraReady(true);
  }, []);

  const handleCameraError = useCallback((message: string) => {
    setCameraReady(false);
    setCameraError(message);
  }, []);

  // Take one photo and return its local URI.
  const captureFrame = useCallback(async (): Promise<string> => {
    if (!cameraRef.current) {
      throw new Error("Camera is not ready.");
    }

    const photo = await cameraRef.current.takePictureAsync({
      quality: 0.5,
      base64: false, // to take the uri and not transform it to string
      skipProcessing: false,
      shutterSound: false, //to make sound of camera picture gone
    });

    if (!photo?.uri) {
      throw new Error("Could not capture a frame.");
    }

    return photo.uri;
  }, []);

  const {
    frameCount,
    uploading,
    lastResponse,
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
      !ending &&
      !questionActive &&
      !questionSending,
    captureFrame,
  });

  useEffect(() => {
    if (!focused || !appActive || stopping) {
      void stopSpeaking();
      return;
    }

    if (!lastResponse) return;

    // Never handle the same automatic response twice.
    if (lastAutomaticFrameRef.current === lastResponse.frameId) {
      return;
    }

    lastAutomaticFrameRef.current = lastResponse.frameId;

    if (
      questionActive ||
      questionActiveRef.current ||
      questionSendingRef.current
    ) {
      return;
    }

    const answer = lastResponse.answer?.trim();

    if (!answer) return;

    setLatestAnswer(answer);

    // Suppress repeated automatic descriptions.
    if (lastSpokenAnswerRef.current === answer) {
      return;
    }

    lastSpokenAnswerRef.current = answer;
    console.log("[TTS]", {
      preferredLanguage,
      responseLanguage: lastResponse.language,
      selectedLanguage: lastResponse.language ?? preferredLanguage,
      answer,
    });
    void speakAnswer(answer, lastResponse.language ?? preferredLanguage).catch(
      console.warn,
    );
  }, [
    lastResponse,
    preferredLanguage,
    focused,
    appActive,
    stopping,
    questionActive,
  ]);

  async function handleRecordingActivity(active: boolean) {
    if (active) {
      if (!visibleRef.current) {
        throw new Error("Assistance is not active.");
      }

      // Immediately stop scheduling automatic frames.
      pause();

      questionActiveRef.current = true;
      setQuestionActive(true);

      // Prevent TTS from being recorded by the microphone.
      await stopSpeaking();

      if (!visibleRef.current) {
        throw new Error("Assistance is no longer active.");
      }
    } else {
      questionActiveRef.current = false;
      setQuestionActive(false);
    }
  }

  async function submitQuestion(audioUri: string) {
    if (!session || !token || !cameraReady || !visibleRef.current || stopping) {
      throw new Error("Assistance is not ready to send a question.");
    }

    if (questionSendingRef.current) return;

    const version = visibilityVersion.current;
    const sessionId = session.sessionId;

    questionSendingRef.current = true;
    setQuestionSending(true);

    try {
      pause();

      // Finish any previous automatic capture/upload.
      await waitForCurrentUpload();

      if (!visibleRef.current || version !== visibilityVersion.current) {
        return;
      }

      // Take a fresh image when the question is ready.
      const imageUri = await captureFrame();

      if (!visibleRef.current || version !== visibilityVersion.current) {
        return;
      }

      // Send the image and WAV in the same request.
      const result = await framesApi.upload(
        sessionId,
        imageUri,
        token,
        audioUri,
      );

      if (!visibleRef.current || version !== visibilityVersion.current) {
        return;
      }

      if (result.sessionId !== sessionId) {
        throw new Error("The response belongs to a different session.");
      }

      setLatestAnswer(result.answer);

      const answer = result.answer?.trim();

      if (answer) {
        console.log("[Question] Speaking answer");

        const completed = await speakAnswer(
          answer,
          result.language ?? preferredLanguage,
        );

        if (
          completed &&
          visibleRef.current &&
          version === visibilityVersion.current
        ) {
          lastSpokenAnswerRef.current = answer;

          console.log("[Question] Answer finished");
        }
      }
    } finally {
      questionSendingRef.current = false;
      setQuestionSending(false);
    }
  }

  async function handleStop() {
    if (
      stopping ||
      !session ||
      questionActiveRef.current ||
      questionSendingRef.current
    ) {
      return;
    }

    // Stop scheduling new frames immediately.
    pause();
    setStopping(true);
    await stopSpeaking();
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
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>VisionAid Assistance</Text>

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
          {cameraVisible && (
            <WavQuestionRecorder
              disabled={!cameraReady || stopping || ending || questionSending}
              onActivityChange={handleRecordingActivity}
              onRecorded={submitQuestion}
            />
          )}

          {questionSending && (
            <Text style={styles.info}>Analyzing your question...</Text>
          )}

          <Text style={styles.info}>
            {stopping
              ? "Finishing the current analysis..."
              : questionSending
                ? "Analyzing your question..."
                : questionActive
                  ? "Automatic analysis paused for question"
                  : uploading
                    ? "Analyzing camera frame..."
                    : cameraReady && cameraVisible
                      ? "Automatic analysis is ready"
                      : "Waiting for camera"}
          </Text>

          <Text style={styles.info}>Frames analyzed: {frameCount}</Text>

          {latestAnswer && (
            <View style={styles.answerContainer}>
              <Text style={styles.answerTitle}>Latest AI Answer</Text>

              <Text style={styles.answer}>{latestAnswer}</Text>
            </View>
          )}

          {cameraError && <Text style={styles.error}>{cameraError}</Text>}

          {frameError && (
            <View style={styles.center}>
              <Text style={styles.error}>{frameError}</Text>

              <Pressable
                style={styles.retryButton}
                onPress={retry}
                accessibilityRole="button"
              >
                <Text style={styles.buttonText}>Retry Analysis</Text>
              </Pressable>
            </View>
          )}

          <Pressable
            style={styles.stopButton}
            onPress={() => void handleStop()}
            disabled={stopping || ending || questionActive || questionSending}
            accessibilityRole="button"
            accessibilityLabel="End assistance session"
          >
            <Text style={styles.buttonText}>
              {stopping || ending ? "Ending..." : "End Session"}
            </Text>
          </Pressable>
        </>
      )}

      {sessionError && <Text style={styles.error}>{sessionError}</Text>}

      {!loading && !session && (
        <Pressable
          style={styles.retryButton}
          onPress={() => router.replace("/home")}
          accessibilityRole="button"
        >
          <Text style={styles.buttonText}>Back to Home</Text>
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
