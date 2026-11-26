
import { useCallback, useState } from "react";
import { useFocusEffect } from "expo-router";
//useFocusEffect executes when the screen becomes active, including when you return to it.
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
  type AccessRequest,
} from "../../services/api/accessRequests";

type Props = {
  token: string;
};

export default function PendingRequests({ token }: Props) {
  const [requests, setRequests] = useState<AccessRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingId, setProcessingId] = useState<number | null>(null);
  const [error, setError] = useState("");

  const loadRequests = useCallback(async () => {
    setLoading(true);
    setError("");

    try {
      const result = await accessRequestsApi.getPending(token);
      setRequests(result);
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : "Could not load monitoring requests."
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useFocusEffect(
  useCallback(() => {
    void loadRequests();
  }, [loadRequests])
);

  async function respond(
    request: AccessRequest,
    accept: boolean
  ) {
    if (processingId !== null) return;

    setProcessingId(request.requestId);

    try {
      if (accept) {
        await accessRequestsApi.accept(token, request.requestId);
      } else {
        await accessRequestsApi.decline(token, request.requestId);
      }

      setRequests((current) =>
        current.filter(
          (item) => item.requestId !== request.requestId
        )
      );

      Alert.alert(
        "Request updated",
        accept
          ? `You granted ${request.adminName} monitoring access.`
          : "Monitoring request declined."
      );
    } catch (err) {
      Alert.alert(
        "Error",
        err instanceof Error ? err.message : "Request failed."
      );
    } finally {
      setProcessingId(null);
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Monitoring Requests</Text>

      <Text style={styles.description}>
        Choose who can monitor your VisionAid sessions.
      </Text>

      <Pressable
        style={styles.refresh}
        onPress={() => void loadRequests()}
      >
        <Text>Refresh requests</Text>
      </Pressable>

      {loading ? (
        <ActivityIndicator size="large" />
      ) : error ? (
        <Text style={styles.error}>{error}</Text>
      ) : (
        <FlatList
          data={requests}
          keyExtractor={(item) => String(item.requestId)}
          ListEmptyComponent={
            <Text>No pending monitoring requests.</Text>
          }
          renderItem={({ item }) => (
            <View style={styles.card}>
              <Text style={styles.name}>{item.adminName}</Text>
              <Text>{item.adminEmail}</Text>

              <Text style={styles.description}>
                This person requests access to monitor your sessions.
              </Text>

              <View style={styles.actions}>
                <Pressable
                  style={[styles.button, styles.accept]}
                  disabled={processingId !== null}
                  accessibilityRole="button"
                  accessibilityLabel={`Accept request from ${item.adminName}`}
                  onPress={() => void respond(item, true)}
                >
                  <Text style={styles.buttonText}>Accept</Text>
                </Pressable>

                <Pressable
                  style={[styles.button, styles.decline]}
                  disabled={processingId !== null}
                  accessibilityRole="button"
                  accessibilityLabel={`Decline request from ${item.adminName}`}
                  onPress={() => void respond(item, false)}
                >
                  <Text style={styles.buttonText}>Decline</Text>
                </Pressable>
              </View>
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
  },
  title: {
    fontSize: 24,
    fontWeight: "bold",
    marginBottom: 8,
  },
  description: {
    color: "#555",
    marginVertical: 10,
  },
  card: {
    backgroundColor: "#fff",
    padding: 18,
    marginVertical: 8,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: "#ddd",
  },
  name: {
    fontSize: 18,
    fontWeight: "bold",
  },
  actions: {
    flexDirection: "row",
    gap: 12,
    marginTop: 16,
  },
  button: {
    padding: 12,
    borderRadius: 8,
    flex: 1,
    alignItems: "center",
  },
  accept: {
    backgroundColor: "#18794e",
  },
  decline: {
    backgroundColor: "#b42318",
  },
  buttonText: {
    color: "white",
    fontWeight: "bold",
  },
  refresh: {
    alignSelf: "flex-start",
    marginBottom: 12,
  },
  error: {
    color: "#b42318",
  },
});
