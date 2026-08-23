import { NavigationContainer } from "@react-navigation/native";
import { ActivityIndicator, View } from "react-native";
import { useAuthInit } from "@concertable/mobile/auth/useAuthInit";
import { useCurrentUser } from "@concertable/mobile/auth/useCurrentUser";
import { isB2bIdentity } from "./identity";
import { ArtistTabs } from "./ArtistTabs";
import { VenueTabs } from "./VenueTabs";

export function RootNavigator() {
  const user = useCurrentUser();
  const isReady = useAuthInit();

  if (!isReady)
    return (
      <View style={{ flex: 1, alignItems: "center", justifyContent: "center" }}>
        <ActivityIndicator size="large" />
      </View>
    );

  return (
    <NavigationContainer>
      {isB2bIdentity(user) &&
      user.memberships.some((m) => m.type === "venue") ? (
        <VenueTabs />
      ) : (
        <ArtistTabs />
      )}
    </NavigationContainer>
  );
}
