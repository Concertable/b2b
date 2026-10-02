import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { Home, Search, User } from "lucide-react-native";
import { ProfileScreen } from "@concertable/mobile/features/user/screens/ProfileScreen";
import { theme } from "@concertable/mobile/lib/theme";
import { PlaceholderScreen } from "../screens/PlaceholderScreen";
import { PUBLIC_TAB_NAMES } from "./rootNavigation";
import type { PublicTabParamList } from "./types";

const Tab = createBottomTabNavigator<PublicTabParamList>();

export function PublicTabs() {
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
        name={PUBLIC_TAB_NAMES.home}
        children={() => <PlaceholderScreen name="Home" />}
        options={{
          tabBarIcon: ({ color, size }) => <Home size={size} color={color} />,
        }}
      />
      <Tab.Screen
        name={PUBLIC_TAB_NAMES.search}
        children={() => <PlaceholderScreen name="Search" />}
        options={{
          tabBarIcon: ({ color, size }) => <Search size={size} color={color} />,
        }}
      />
      <Tab.Screen
        name={PUBLIC_TAB_NAMES.account}
        component={ProfileScreen}
        options={{
          tabBarIcon: ({ color, size }) => <User size={size} color={color} />,
        }}
      />
    </Tab.Navigator>
  );
}
