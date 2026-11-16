
import { useEffect, useState } from "react";
import { Button, Text, View } from "react-native";

import {
  useAudioStream,
  requestRecordingPermissionsAsync,
} from "expo-audio";

type Props = {
  onRecorded: (audioUri: string) => void;
};

export default function WavQuestionRecorder({
  onRecorded,
}: Props) {
  const { stream } = useAudioStream({
    sampleRate: 16000,
    channels: 1,
    encoding: "int16",
  });

  const [recording, setRecording] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      stream.stop();
    };
  }, [stream]);

  async function startRecording() {
    if (recording || busy) return;

    setBusy(true);
    setError(null);

    try {
      const permission =
        await requestRecordingPermissionsAsync();

      if (!permission.granted) {
        setError("Microphone permission is required.");
        return;
      }

      await stream.start();

      await stream.startFileRecordingAsync({
        format: "wav",
      });

      setRecording(true);
    } catch (err) {
      stream.stop();
      setError(
        err instanceof Error
          ? err.message
          : "Could not start recording."
      );
    } finally {
      setBusy(false);
    }
  }

  async function stopRecording() {
    if (!recording || busy) return;

    setBusy(true);

    try {
      const result =
        await stream.stopFileRecordingAsync();

      stream.stop();
      setRecording(false);

      // Return the recorded WAV file location.
      onRecorded(result.uri);
    } catch (err) {
      stream.stop();
      setRecording(false);
      setError(
        err instanceof Error
          ? err.message
          : "Could not save recording."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <View>
      <Button
        title={recording ? "Stop Question" : "Ask Question"}
        onPress={
          recording ? stopRecording : startRecording
        }
        disabled={busy}
      />

      {recording && (
        <Text>Recording your question...</Text>
      )}

      {error && <Text>{error}</Text>}
    </View>
  );
}
