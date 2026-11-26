import { Redirect } from "expo-router";
import { ActivityIndicator, View } from "react-native";

import PendingRequests from "../../components/monitoring/PendingRequests";
import { useAuth } from "../../contexts/AuthContext";

export default function MonitoringRequestsScreen() {
  const { token, loading } = useAuth();

  // The app may still be restoring the saved login.
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

  // A logged-out user cannot view monitoring requests.
  if (!token) {
    return <Redirect href="/(auth)/login" />;
  }

  // The user is authenticated.
  return <PendingRequests token={token} />;
}
