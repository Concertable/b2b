import { MyVenueScreen } from "../features/venues/screens/MyVenueScreen";
import { createAppStack } from "shared/navigation/createAppStack";
import type { MyVenueStackParamList } from "./types";

const Stack = createAppStack<MyVenueStackParamList>();

export function MyVenueStack() {
  return (
    <Stack.Navigator>
      <Stack.Screen
        name="MyVenueMain"
        component={MyVenueScreen}
        options={{ title: "My Venue" }}
      />
    </Stack.Navigator>
  );
}
