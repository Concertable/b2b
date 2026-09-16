import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { Home, MessageCircle, User } from "lucide-react-native";
import { PlaceholderScreen } from "../screens/PlaceholderScreen";
import { ProfileStack } from "./ProfileStack";
import { theme } from "@concertable/mobile/lib/theme";
import type { BusinessTabParamList } from "./types";

const Tab = createBottomTabNavigator<BusinessTabParamList>();

/**
 * The neutral surface every selected business can reach, whatever it has activated. A promoter, an agency
 * or a production business has neither a venue nor an act, and previously fell through to the artist tabs
 * and was offered a profile it does not have.
 */
export function BusinessTabs() {
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
