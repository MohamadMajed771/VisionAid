
import { useState } from "react";
import { Redirect, router} from "expo-router";

import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from "react-native";

import PendingRequests
  from "../../components/monitoring/PendingRequests";

import AcceptedMonitors
  from "../../components/monitoring/AcceptedMonitors";

import { useAuth } from "../../contexts/AuthContext";

export default function MonitoringRequestsScreen() {
  const { token, loading } = useAuth();

  const [tab, setTab] =
    useState<"pending" | "accepted">("pending");

  if (loading) {
    return (
      <View style={styles.loading}>
        <ActivityIndicator size="large" />
      </View>
    );
  }

  if (!token) {
    return <Redirect href="/(auth)/login" />;
  }

  return (
    <View style={styles.container}>

      <Pressable
        onPress={() => router.back()}
        accessibilityRole="button"
        accessibilityLabel="Back to Home"
      >
  <Text style={{ padding: 16, color: "#175CD3" }}>
    Back
  </Text>
</Pressable>

      <View style={styles.tabs}>

        <Pressable
          style={[
            styles.tab,
            tab === "pending" && styles.activeTab,
          ]}
          onPress={() => setTab("pending")}
          accessibilityRole="tab"
          accessibilityState={{
            selected: tab === "pending",
          }}
        >
          <Text
            style={[
              styles.tabText,
              tab === "pending" && styles.activeText,
            ]}
          >
            Pending Requests
          </Text>
        </Pressable>

        <Pressable
          style={[
            styles.tab,
            tab === "accepted" && styles.activeTab,
          ]}
          onPress={() => setTab("accepted")}
          accessibilityRole="tab"
          accessibilityState={{
            selected: tab === "accepted",
          }}
        >
          <Text
            style={[
              styles.tabText,
              tab === "accepted" && styles.activeText,
            ]}
          >
            My Monitors
          </Text>
        </Pressable>

      </View>

      {tab === "pending" ? (
        <PendingRequests token={token} />
      ) : (
        <AcceptedMonitors token={token} />
      )}

    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#F4F7FB",
  },
  loading: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
  },
  tabs: {
    flexDirection: "row",
    padding: 12,
    gap: 10,
  },
  tab: {
    flex: 1,
    padding: 16,
    backgroundColor: "#E2E8F0",
    borderRadius: 10,
    alignItems: "center",
    justifyContent: "center",
  },
  activeTab: {
    backgroundColor: "#175CD3",
  },
  tabText: {
    color: "#475569",
    fontSize: 16,
    fontWeight: "600",
    textAlign: "center",
  },
  activeText: {
    color: "#FFFFFF",
  },
});
