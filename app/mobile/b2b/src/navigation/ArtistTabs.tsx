import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { Home, MessageCircle, Music2, Search, User } from "lucide-react-native";
import { PlaceholderScreen } from "../screens/PlaceholderScreen";
import { MyArtistStack } from "./MyArtistStack";
import { ProfileStack } from "./ProfileStack";
import { theme } from "shared/lib/theme";
import type { ArtistTabParamList } from "./types";

const Tab = createBottomTabNavigator<ArtistTabParamList>();

export function ArtistTabs() {
  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: theme.primary,
        tabBarInactiveTintColor: theme.mutedForeground,
        tabBarStyle: { borderTopColor: theme.border },
      }}
    >
      <Tab.Screen
        name="Home"
        children={() => <PlaceholderScreen name="Home" />}
        options={{
          tabBarIcon: ({ color, size }) => <Home size={size} color={color} />,
        }}
      />
      <Tab.Screen
        name="Search"
        children={() => <PlaceholderScreen name="Search" />}
        options={{
          tabBarIcon: ({ color, size }) => <Search size={size} color={color} />,
        }}
      />
      <Tab.Screen
        name="MyArtistTab"
        component={MyArtistStack}
        options={{
          title: "My Artist",
          tabBarIcon: ({ color, size }) => <Music2 size={size} color={color} />,
        }}
      />
      <Tab.Screen
        name="Messages"
        children={() => <PlaceholderScreen name="Messages" />}
        options={{
          tabBarIcon: ({ color, size }) => (
            <MessageCircle size={size} color={color} />
          ),
        }}
      />
      <Tab.Screen
        name="ProfileTab"
        component={ProfileStack}
        options={{
          title: "Profile",
          tabBarIcon: ({ color, size }) => <User size={size} color={color} />,
        }}
      />
    </Tab.Navigator>
  );
}
