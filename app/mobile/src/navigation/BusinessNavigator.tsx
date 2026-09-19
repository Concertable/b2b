import { createBottomTabNavigator } from "@react-navigation/bottom-tabs";
import { BriefcaseBusiness, Building2, MessageCircle, Music2, Settings } from "lucide-react-native";
import type {
  TenantBusinessActivity,
  TenantPermission,
} from "@concertable/b2b/features/tenant/types";
import { ConversationsScreen } from "../features/business/screens/ConversationsScreen";
import { OperationsScreen } from "../features/business/screens/OperationsScreen";
import { OrganizationScreen } from "../features/business/screens/OrganizationScreen";
import { MyArtistStack } from "./MyArtistStack";
import { MyVenueStack } from "./MyVenueStack";
import { theme } from "@concertable/mobile/lib/theme";
import type { BusinessTabParamList } from "./types";

const Tab = createBottomTabNavigator<BusinessTabParamList>();

export function BusinessNavigator({
  activities,
  permissions,
}: Readonly<{
  activities: ReadonlyArray<TenantBusinessActivity>;
  permissions: ReadonlySet<TenantPermission>;
}>) {
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
        name="Operations"
        component={OperationsScreen}
        options={{
          tabBarIcon: ({ color, size }) => (
            <BriefcaseBusiness size={size} color={color} />
          ),
        }}
      />
      {activities.includes("venueOperator") ? (
        <Tab.Screen
          name="Venue"
          component={MyVenueStack}
          options={{
            tabBarIcon: ({ color, size }) => (
              <Building2 size={size} color={color} />
            ),
          }}
        />
      ) : null}
      {activities.includes("artist") ? (
        <Tab.Screen
          name="Artist"
          component={MyArtistStack}
          options={{
            tabBarIcon: ({ color, size }) => (
              <Music2 size={size} color={color} />
            ),
          }}
        />
      ) : null}
      {permissions.has("messages.read") ? (
        <Tab.Screen
          name="Messages"
          component={ConversationsScreen}
          options={{
            tabBarIcon: ({ color, size }) => (
              <MessageCircle size={size} color={color} />
            ),
          }}
        />
      ) : null}
      <Tab.Screen
        name="Organization"
        component={OrganizationScreen}
        options={{
          tabBarIcon: ({ color, size }) => <Settings size={size} color={color} />,
        }}
      />
    </Tab.Navigator>
  );
}
