import { ProfileScreen } from "shared/features/user/screens/ProfileScreen";
import { EditProfileScreen } from "shared/features/user/screens/EditProfileScreen";
import { LocationScreen } from "shared/features/user/screens/LocationScreen";
import { createAppStack } from "shared/navigation/createAppStack";
import type { ProfileStackParamList } from "shared/navigation/types";

const Stack = createAppStack<ProfileStackParamList>();

export function ProfileStack() {
  return (
    <Stack.Navigator>
      <Stack.Screen name="ProfileMain" component={ProfileScreen} options={{ headerShown: false }} />
      <Stack.Screen name="EditProfile" component={EditProfileScreen} options={{ title: "Edit Profile" }} />
      <Stack.Screen name="Location" component={LocationScreen} options={{ title: "Location" }} />
    </Stack.Navigator>
  );
}
