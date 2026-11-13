
import type { RefObject } from "react";

import {
  CameraView,
  useCameraPermissions,
} from "expo-camera";

import {
  View,
  Text,
  Pressable,
  StyleSheet,
} from "react-native";

interface Props {
  cameraRef: RefObject<CameraView | null>;
  onReady: () => void;
  onError: (message: string) => void;
}

export default function CameraPreview({
  cameraRef,
  onReady,
  onError,
}: Props) {
  const [permission, requestPermission] =
    useCameraPermissions();

  // Permission status is loading.
  if (!permission) {
    return (
      <View style={styles.messageContainer}>
        <Text>Checking camera permission...</Text>
      </View>
    );
  }

  // User has not granted camera permission.
  if (!permission.granted) {
    return (
      <View style={styles.messageContainer}>
        <Text style={styles.message}>
          VisionAid needs camera access to analyze
          the surroundings.
        </Text>

        <Pressable
          style={styles.permissionButton}
          onPress={() => void requestPermission()}
          accessibilityRole="button"
        >
          <Text style={styles.buttonText}>
            Allow Camera
          </Text>
        </Pressable>
      </View>
    );
  }

  return (
    <CameraView
      ref={cameraRef}
      style={styles.camera}
      facing="back"
      onCameraReady={onReady}
      onMountError={(event) =>
        onError(event.message)
      }
    />
  );
}

const styles = StyleSheet.create({
  camera: {
    flex: 1,
  },
  messageContainer: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 20,
    gap: 16,
  },
  message: {
    fontSize: 17,
    textAlign: "center",
  },
  permissionButton: {
    backgroundColor: "#175CD3",
    padding: 16,
    borderRadius: 12,
  },
  buttonText: {
    color: "white",
    fontSize: 18,
    fontWeight: "bold",
  },
});
