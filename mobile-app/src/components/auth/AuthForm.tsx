
import { useState } from "react";
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";

import { Link } from "expo-router";
import { useAuth } from "../../contexts/AuthContext";

type Props = {
  mode: "login" | "register";
};

export default function AuthForm({ mode }: Props) {
  const isRegister = mode === "register";
  const { login, register } = useAuth();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] =
    useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function handleSubmit() {
    setError("");

    const cleanEmail = email.trim().toLowerCase();

    if (!cleanEmail || !password) {
      setError("Email and password are required.");
      return;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailRegex.test(cleanEmail)) {
      setError("Please enter a valid email address.");
      return;
    }
    if (isRegister) {
      if (!name.trim()) {
        setError("Name is required.");
        return;
      }

      if (password.length < 8) {
        setError("Password must contain at least 8 characters.");
        return;
      }

      if (password !== confirmPassword) {
        setError("Passwords do not match.");
        return;
      }
    }

    try {
      setBusy(true);

      
    if (isRegister) {
      await register({
        name: name.trim(),
        email: cleanEmail,
        password,
        preferredLanguage: "en",
      });
    } else {
      await login(cleanEmail, password);
    }

    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "Something went wrong."
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <ScrollView
      keyboardShouldPersistTaps="handled"
      contentContainerStyle={styles.container}
    >
      <View style={styles.card}>
        <Text style={styles.title}>VisionAid</Text>

        <Text style={styles.subtitle}>
          {isRegister
            ? "Create your account"
            : "Welcome back"}
        </Text>

        {isRegister && (
          <TextInput
            style={styles.input}
            placeholder="Full name"
            value={name}
            onChangeText={setName}
            autoComplete="name"
            accessibilityLabel="Full name"
          />
        )}

        <TextInput
          style={styles.input}
          placeholder="Email"
          value={email}
          onChangeText={setEmail}
          keyboardType="email-address"
          autoCapitalize="none"
          autoComplete="email"
          accessibilityLabel="Email"
        />

        <TextInput
          style={styles.input}
          placeholder="Password"
          value={password}
          onChangeText={setPassword}
          secureTextEntry
          accessibilityLabel="Password"
        />

        {isRegister && (
          <TextInput
            style={styles.input}
            placeholder="Confirm password"
            value={confirmPassword}
            onChangeText={setConfirmPassword}
            secureTextEntry
            accessibilityLabel="Confirm password"
          />
        )}

        {!!error && (
          <Text
            style={styles.error}
            accessibilityRole="alert"
          >
            {error}
          </Text>
        )}

        <Pressable
          style={styles.button}
          disabled={busy}
          onPress={handleSubmit}
          accessibilityRole="button"
        >
          {busy ? (
            <ActivityIndicator color="#fff" />
          ) : (
            <Text style={styles.buttonText}>
              {isRegister ? "Create account" : "Login"}
            </Text>
          )}
        </Pressable>

        <Link
          href={isRegister ? "/login" : "/register"}
          style={styles.link}
        >
          {isRegister
            ? "Already have an account? Login"
            : "Don't have an account? Register"}
        </Link>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: {
    flexGrow: 1,
    justifyContent: "center",
    backgroundColor: "#F4F7FB",
    padding: 24,
  },
  card: {
    padding: 24,
    backgroundColor: "#FFFFFF",
    borderRadius: 18,
    gap: 14,
  },
  title: {
    fontSize: 32,
    fontWeight: "bold",
    color: "#17375E",
    textAlign: "center",
  },
  subtitle: {
    fontSize: 18,
    textAlign: "center",
    color: "#475569",
    marginBottom: 10,
  },
  input: {
    borderWidth: 1,
    borderColor: "#94A3B8",
    borderRadius: 10,
    padding: 15,
    fontSize: 17,
    color: "#111827",
  },
  button: {
    backgroundColor: "#175CD3",
    padding: 17,
    borderRadius: 10,
    alignItems: "center",
  },
  buttonText: {
    color: "#FFFFFF",
    fontWeight: "bold",
    fontSize: 18,
  },
  error: {
    color: "#B42318",
    fontSize: 15,
  },
  link: {
    color: "#175CD3",
    fontSize: 16,
    textAlign: "center",
    marginTop: 8,
  },
});
