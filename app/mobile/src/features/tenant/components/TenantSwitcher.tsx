import { ScrollView, View } from "react-native";
import type { Membership } from "@concertable/b2b/features/tenant/types";
import { Button } from "@concertable/mobile/components/ui/button";
import { Text } from "@concertable/mobile/components/ui/text";

interface TenantSwitcherProps {
  readonly activeMembership: Membership;
  readonly memberships: ReadonlyArray<Membership>;
  readonly onSelect: (tenantId: string) => void;
}

export function TenantSwitcher({
  activeMembership,
  memberships,
  onSelect,
}: Readonly<TenantSwitcherProps>) {
  if (memberships.length <= 1) return null;

  return (
    <View className="border-b border-border bg-card px-4 py-2">
      <Text className="mb-2 text-xs font-medium text-muted-foreground">
        Managing {activeMembership.legalName}
      </Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false}>
        <View className="flex-row gap-2">
          {memberships.map((membership) => (
            <Button
              key={membership.tenantId}
              size="sm"
              variant={
                membership.tenantId === activeMembership.tenantId
                  ? "default"
                  : "outline"
              }
              onPress={() => onSelect(membership.tenantId)}
              accessibilityLabel={`Switch to ${membership.legalName}`}
            >
              <Text>{membership.legalName}</Text>
            </Button>
          ))}
        </View>
      </ScrollView>
    </View>
  );
}
