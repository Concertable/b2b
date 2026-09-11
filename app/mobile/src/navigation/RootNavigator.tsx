import { useCallback, useState } from "react";
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
import { Button } from "@concertable/mobile/components/ui/button";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import { ActiveTenantProvider } from "../features/tenant/ActiveTenantContext";
import { TenantChooser } from "../features/tenant/components/TenantChooser";
import { TenantSwitcher } from "../features/tenant/components/TenantSwitcher";
import { initializeTenantSession } from "../lib/b2bClient";
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
  const [selectionError, setSelectionError] = useState(false);
  const selectTenant = useCallback(
    async (tenantId: string) => {
      setSelectionError(false);
      try {
        await tenant.selectTenant(tenantId);
        await queryClient.invalidateQueries();
      } catch {
        setSelectionError(true);
      }
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
        disabled={tenant.isSelectionPending}
        onSelect={(tenantId) => void selectTenant(tenantId)}
      />
    );
  }

  return (
    <View className="flex-1 bg-background">
      <TenantSwitcher
        activeMembership={tenant.activeMembership}
        memberships={tenant.memberships}
        disabled={tenant.isSelectionPending}
        onSelect={(tenantId) => void selectTenant(tenantId)}
      />
      {selectionError ? (
        <Text className="px-4 py-2 text-center text-destructive">
          Failed to switch organization. Try again.
        </Text>
      ) : null}
      <ActiveTenantProvider tenantId={tenant.activeMembership.tenantId}>
        <NavigationContainer key={tenant.activeMembership.tenantId}>
          {tenant.activeMembership.type === "venue" ? (
            <VenueTabs />
          ) : (
            <ArtistTabs />
          )}
        </NavigationContainer>
      </ActiveTenantProvider>
    </View>
  );
}

export function RootNavigator() {
  const user = useCurrentUser();
  const isAuthReady = useAuthInit();
  const [tenantInitialization, setTenantInitialization] = useState<
    "loading" | "ready" | "error"
  >("loading");

  const initialize = useCallback(() => {
    setTenantInitialization("loading");
    void initializeTenantSession().then(
      () => setTenantInitialization("ready"),
      () => setTenantInitialization("error"),
    );
  }, []);

  useMountEffect(initialize);

  if (!isAuthReady || tenantInitialization === "loading")
    return <LoadingScreen />;

  if (tenantInitialization === "error") {
    return (
      <View className="flex-1 items-center justify-center gap-4 bg-background px-6">
        <Text className="text-center text-destructive">
          Failed to restore your organization session.
        </Text>
        <Button onPress={initialize} accessibilityLabel="Retry organization session">
          <Text>Retry</Text>
        </Button>
      </View>
    );
  }

  if (user === undefined) {
    return (
      <NavigationContainer>
        <ArtistTabs />
      </NavigationContainer>
    );
  }

  return <AuthenticatedNavigator />;
}
