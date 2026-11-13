
import { Redirect } from "expo-router";
import {
  ActivityIndicator,
  View,
} from "react-native";

import { useAuth } from "../contexts/AuthContext";

export default function Index() {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <View
        style={{
          flex: 1,
          justifyContent: "center",
          alignItems: "center",
        }}
      >
        <ActivityIndicator size="large" />
      </View>
    );
  }

  return (
    <Redirect
      href={user ? "/home" : "/login"}
    />
  );
}
