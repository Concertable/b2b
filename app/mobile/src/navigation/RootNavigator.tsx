import { useCallback, useEffect, useState } from "react";
import { NavigationContainer } from "@react-navigation/native";
import { useQueryClient } from "@tanstack/react-query";
import { ActivityIndicator, View } from "react-native";
import {
  useB2bIdentityQuery,
  useTenant,
} from "@concertable/b2b/features/tenant";
import { useAuthInit } from "@concertable/mobile/auth/useAuthInit";
import { useCurrentUser } from "@concertable/mobile/auth/useCurrentUser";
import { Text } from "@concertable/mobile/components/ui/text";
import { TenantChooser } from "../features/tenant/components/TenantChooser";
import { TenantSwitcher } from "../features/tenant/components/TenantSwitcher";
import { tenantSessionReady } from "../lib/b2bClient";
import { ArtistTabs } from "./ArtistTabs";
import { VenueTabs } from "./VenueTabs";

function LoadingScreen() {
  return (
    <View className="flex-1 items-center justify-center bg-background">
      <ActivityIndicator size="large" />
    </View>
  );
}

function AuthenticatedNavigator() {
  const queryClient = useQueryClient();
  const identityQuery = useB2bIdentityQuery();
  const tenant = useTenant(identityQuery.data?.memberships ?? []);
  const selectTenant = useCallback(
    async (tenantId: string) => {
      await tenant.selectTenant(tenantId);
      await queryClient.invalidateQueries();
    },
    [queryClient, tenant.selectTenant],
  );

  if (identityQuery.isLoading) return <LoadingScreen />;
  if (identityQuery.isError || identityQuery.data === undefined) {
    return (
      <View className="flex-1 items-center justify-center bg-background px-6">
        <Text className="text-center text-destructive">
          Failed to load your organization memberships.
        </Text>
      </View>
    );
  }

  if (tenant.memberships.length === 0) {
    return (
      <View className="flex-1 items-center justify-center bg-background px-6">
        <Text className="text-center text-muted-foreground">
          You do not have an active artist or venue membership.
        </Text>
      </View>
    );
  }

  if (tenant.selectionRequired || tenant.activeMembership === undefined) {
    return (
      <TenantChooser
        memberships={tenant.memberships}
        onSelect={(tenantId) => void selectTenant(tenantId)}
      />
    );
  }

  return (
    <View className="flex-1 bg-background">
      <TenantSwitcher
        activeMembership={tenant.activeMembership}
        memberships={tenant.memberships}
        onSelect={(tenantId) => void selectTenant(tenantId)}
      />
      <NavigationContainer>
        {tenant.activeMembership.type === "venue" ? (
          <VenueTabs />
        ) : (
          <ArtistTabs />
        )}
      </NavigationContainer>
    </View>
  );
}

export function RootNavigator() {
  const user = useCurrentUser();
  const isAuthReady = useAuthInit();
  const [isTenantReady, setIsTenantReady] = useState(false);

  useEffect(() => {
    void tenantSessionReady.then(() => setIsTenantReady(true));
  }, []);

  if (!isAuthReady || !isTenantReady) return <LoadingScreen />;

  if (user === undefined) {
    return (
      <NavigationContainer>
        <ArtistTabs />
      </NavigationContainer>
    );
  }

  return <AuthenticatedNavigator />;
}
