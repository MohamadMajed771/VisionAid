
import { useState } from "react";
import { router } from "expo-router";

import {
  ActivityIndicator,
  Alert,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from "react-native";

import { useAuth } from "../../contexts/AuthContext";
import type { PreferredLanguage } from "../../types/auth";

export default function SettingsScreen() {
  const { user, updatePreferredLanguage } = useAuth();

  const [busy, setBusy] = useState(false);

  async function changeLanguage(
    language: PreferredLanguage
  ) {
    if (!user || busy) return;

    if (language === user.preferredLanguage) return;

    setBusy(true);

    try {
      await updatePreferredLanguage(language);

      Alert.alert(
        "Language updated",
        language === "fr"
          ? "French selected."
          : "English selected."
      );
    } catch (err) {
      Alert.alert(
        "Error",
        err instanceof Error
          ? err.message
          : "Could not update language."
      );
    } finally {
      setBusy(false);
    }
  }

  if (!user) return null;

  return (
    <ScrollView
      contentContainerStyle={styles.container}
    >
      <Pressable
        onPress={() => router.back()}
        disabled={busy}
        accessibilityRole="button"
      >
        <Text style={styles.back}>Back to Home</Text>
      </Pressable>

      <Text style={styles.title}>Settings</Text>

      <View style={styles.card}>
        <Text style={styles.heading}>Your account</Text>

        <Text style={styles.label}>Name</Text>
        <Text style={styles.value}>{user.name}</Text>

        <Text style={styles.label}>Email</Text>
        <Text style={styles.value}>{user.email}</Text>
      </View>

      <View style={styles.card}>
        <Text style={styles.heading}>
          Preferred language
        </Text>

        <Text style={styles.label}>
          Language used for AI answers and speech.
        </Text>

        <Pressable
          style={[
            styles.option,
            user.preferredLanguage === "en" &&
              styles.selected,
          ]}
          disabled={busy}
          onPress={() => void changeLanguage("en")}
          accessibilityRole="radio"
          accessibilityState={{
            checked: user.preferredLanguage === "en",
            disabled: busy,
          }}
        >
          <Text style={styles.value}>English</Text>
          {user.preferredLanguage === "en" && (
            <Text style={styles.selectedText}>Selected</Text>
          )}
        </Pressable>

        <Pressable
          style={[
            styles.option,
            user.preferredLanguage === "fr" &&
              styles.selected,
          ]}
          disabled={busy}
          onPress={() => void changeLanguage("fr")}
          accessibilityRole="radio"
          accessibilityState={{
            checked: user.preferredLanguage === "fr",
            disabled: busy,
          }}
        >
          <Text style={styles.value}>Français</Text>
          {user.preferredLanguage === "fr" && (
            <Text style={styles.selectedText}>Selected</Text>
          )}
        </Pressable>

        {busy && <ActivityIndicator size="large" />}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    padding: 24,
    gap: 20,
    backgroundColor: "#F4F7FB",
  },
  back: {
    fontSize: 18,
    color: "#175CD3",
    paddingVertical: 12,
  },
  title: {
    fontSize: 32,
    fontWeight: "bold",
    color: "#17375E",
  },
  card: {
    padding: 20,
    gap: 12,
    borderRadius: 14,
    backgroundColor: "#FFFFFF",
    borderWidth: 1,
    borderColor: "#E2E8F0",
  },
  heading: {
    fontSize: 22,
    fontWeight: "bold",
    color: "#17375E",
  },
  label: {
    fontSize: 16,
    color: "#475569",
  },
  value: {
    fontSize: 19,
    color: "#17375E",
  },
  option: {
    padding: 18,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: "#94A3B8",
    gap: 6,
  },
  selected: {
    backgroundColor: "#EAF2FF",
    borderColor: "#175CD3",
    borderWidth: 2,
  },
  selectedText: {
    color: "#175CD3",
    fontWeight: "600",
  },
});
