
import {
  View,
  Text,
  Pressable,
  StyleSheet,
} from "react-native";

import { router } from "expo-router";
import { useAuth } from "../../contexts/AuthContext";

export default function HomeScreen() {
  const { user, logout } = useAuth();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>VisionAid</Text>

      <Text style={styles.welcome}>
        Welcome, {user?.name}
      </Text>

      <Text style={styles.description}>
        Your visual assistance application.
      </Text>

      <Pressable
        style={styles.startButton}
        onPress={() => router.push("/assistance")}
        accessibilityRole="button"
        accessibilityLabel="Start Assistance"
      >
        <Text style={styles.buttonText}>
          Start Assistance
        </Text>
      </Pressable>

      <Pressable
        style={styles.logoutButton}
        onPress={logout}
        accessibilityRole="button" 
        accessibilityLabel="Logout"
      >
        <Text style={styles.logoutText}>
          Logout
        </Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: "center",
    padding: 24,
    backgroundColor: "#F4F7FB",
    gap: 20,
  },
  title: {
    fontSize: 36,
    fontWeight: "bold",
    color: "#17375E",
    textAlign: "center",
  },
  welcome: {
    fontSize: 24,
    fontWeight: "600",
    textAlign: "center",
  },
  description: {
    fontSize: 18,
    textAlign: "center",
    color: "#475569",
  },
  startButton: {
    backgroundColor: "#175CD3",
    padding: 22,
    borderRadius: 14,
    alignItems: "center",
  },
  buttonText: {
    color: "white",
    fontSize: 22,
    fontWeight: "bold",
  },
  logoutButton: {
    padding: 15,
    alignItems: "center",
  },
  logoutText: {
    color: "#B42318",
    fontSize: 17,
  },
});
