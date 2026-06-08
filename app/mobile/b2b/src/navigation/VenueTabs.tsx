import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { Building2, Home, MessageCircle, User } from "lucide-react-native";
import { PlaceholderScreen } from "../screens/PlaceholderScreen";
import { MyVenueStack } from "./MyVenueStack";
import { ProfileStack } from "./ProfileStack";
import { theme } from "shared/lib/theme";
import type { VenueTabParamList } from "./types";

const Tab = createBottomTabNavigator<VenueTabParamList>();

export function VenueTabs() {
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
        name="MyVenueTab"
        component={MyVenueStack}
        options={{
          title: "My Venue",
          tabBarIcon: ({ color, size }) => (
            <Building2 size={size} color={color} />
          ),
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
