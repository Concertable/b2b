import { NavigationContainer } from "@react-navigation/native";
import { ActivityIndicator, View } from "react-native";
import { useAuthInit } from "shared/auth/useAuthInit";
import { useAuthStore } from "@concertable/shared/features/auth";
import { ArtistTabs } from "./ArtistTabs";
import { VenueTabs } from "./VenueTabs";

export function RootNavigator() {
  const user = useAuthStore((s) => s.user);
  const isReady = useAuthInit();

  if (!isReady)
    return (
      <View style={{ flex: 1, alignItems: "center", justifyContent: "center" }}>
        <ActivityIndicator size="large" />
      </View>
    );

  return (
    <NavigationContainer>
      {user?.role === "VenueManager" ? <VenueTabs /> : <ArtistTabs />}
    </NavigationContainer>
  );
}
