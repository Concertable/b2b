import { MyArtistScreen } from "../features/artists/screens/MyArtistScreen";
import { createAppStack } from "@concertable/mobile/navigation/createAppStack";
import type { MyArtistStackParamList } from "./types";

const Stack = createAppStack<MyArtistStackParamList>();

export function MyArtistStack() {
  return (
    <Stack.Navigator>
      <Stack.Screen
        name="MyArtistMain"
        component={MyArtistScreen}
        options={{ title: "My Artist" }}
      />
    </Stack.Navigator>
  );
}
