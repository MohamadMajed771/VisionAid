
import { useCallback, useState } from "react";
import { useFocusEffect } from "expo-router";

import {
  ActivityIndicator,
  Alert,
  FlatList,
  Pressable,
  StyleSheet,
  Text,
  View,
} from "react-native";

import {
  accessRequestsApi,
  type AcceptedMonitor,
} from "../../services/api/accessRequests";

type Props = {
  token: string;
};

export default function AcceptedMonitors({ token }: Props) {

  const [monitors, setMonitors] =
    useState<AcceptedMonitor[]>([]);

  const [loading, setLoading] = useState(true);

  const [processingId, setProcessingId] =
    useState<number | null>(null);

  const [error, setError] = useState("");

  // Retrieve all accepted monitors.
  const loadMonitors = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const result =
        await accessRequestsApi.getAcceptedMonitors(token);

      setMonitors(result);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Could not load monitors."
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  // Load monitors when the screen becomes active.
  useFocusEffect(
    useCallback(() => {
      void loadMonitors();
    }, [loadMonitors])
  );

  // Remove monitoring permission.
  async function revokeMonitor(monitor: AcceptedMonitor) {
    if (processingId !== null) return;

    setProcessingId(monitor.adminId);

    try {
      await accessRequestsApi.revoke(
        token,
        monitor.adminId
      );

      setMonitors((current) =>
        current.filter(
          (item) => item.adminId !== monitor.adminId
        )
      );

      Alert.alert(
        "Access revoked",
        `${monitor.name} no longer has monitoring access.`
      );
    } catch (err) {
      Alert.alert(
        "Error",
        err instanceof Error
          ? err.message
          : "Could not revoke access."
      );
    } finally {
      setProcessingId(null);
    }
  }

  // Ask for confirmation before removing access.
  function confirmRevoke(monitor: AcceptedMonitor) {
    Alert.alert(
      "Revoke access?",
      `Remove monitoring access for ${monitor.name}?`,
      [
        {
          text: "Cancel",
          style: "cancel",
        },
        {
          text: "Revoke",
          style: "destructive",
          onPress: () => void revokeMonitor(monitor),
        },
      ]
    );
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>My Monitors</Text>

      <Text style={styles.description}>
        People who currently have access to your sessions.
      </Text>

      <Pressable
        style={styles.refresh}
        disabled={loading || processingId !== null}
        onPress={() => void loadMonitors()}
        accessibilityRole="button"
      >
        <Text style={styles.refreshText}>
          Refresh monitors
        </Text>
      </Pressable>

      {loading ? (
        <ActivityIndicator size="large" />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : (
        <FlatList
          data={monitors}
          keyExtractor={(item) => String(item.adminId)}
          ListEmptyComponent={
            <Text>You have no accepted monitors.</Text>
          }
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Text style={styles.name}>
                {item.name}
              </Text>

              <Text>{item.email}</Text>

              <Text style={styles.description}>
                This person has monitoring access.
              </Text>

              <Pressable
                style={[
                  styles.revokeButton,
                  processingId !== null &&
                    styles.disabledButton,
                ]}
                disabled={processingId !== null}
                onPress={() => confirmRevoke(item)}
                accessibilityRole="button"
                accessibilityLabel={
                  `Revoke access for ${item.name}`
                }
              >
                <Text style={styles.buttonText}>
                  {processingId === item.adminId
                    ? "Removing..."
                    : "Revoke access"}
                </Text>
              </Pressable>
            </View>
          )}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: "#F4F7FB",
  },
  title: {
    fontSize: 24,
    fontWeight: "bold",
    color: "#17375E",
  },
  description: {
    color: "#475569",
    fontSize: 16,
    marginVertical: 10,
  },
  refresh: {
    paddingVertical: 12,
    marginBottom: 10,
  },
  refreshText: {
    color: "#175CD3",
    fontSize: 16,
    fontWeight: "600",
  },
  card: {
    backgroundColor: "#FFFFFF",
    borderRadius: 14,
    padding: 18,
    marginVertical: 8,
    borderWidth: 1,
    borderColor: "#E2E8F0",
  },
  name: {
    fontSize: 20,
    fontWeight: "bold",
    color: "#17375E",
  },
  revokeButton: {
    backgroundColor: "#B42318",
    padding: 15,
    borderRadius: 10,
    alignItems: "center",
    marginTop: 10,
  },
  disabledButton: {
    opacity: 0.5,
  },
  buttonText: {
    color: "#FFFFFF",
    fontWeight: "bold",
    fontSize: 16,
  },
  error: {
    color: "#B42318",
  },
});
