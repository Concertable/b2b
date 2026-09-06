import { View } from "react-native";
import type { Membership } from "@concertable/b2b/features/tenant/types";
import { Button } from "@concertable/mobile/components/ui/button";
import { Screen } from "@concertable/mobile/components/ui/Screen";
import { Text } from "@concertable/mobile/components/ui/text";

interface TenantChooserProps {
  readonly memberships: ReadonlyArray<Membership>;
  readonly onSelect: (tenantId: string) => void;
}

export function TenantChooser({
  memberships,
  onSelect,
}: Readonly<TenantChooserProps>) {
  return (
    <Screen>
      <View className="flex-1 justify-center gap-6 px-4">
        <View className="gap-2">
          <Text className="text-center text-2xl font-bold text-foreground">
            Choose your organization
          </Text>
          <Text className="text-center text-sm text-muted-foreground">
            Pick the artist or venue you want to manage. You can switch at any
            time.
          </Text>
        </View>
        <View className="gap-3">
          {memberships.map((membership) => (
            <Button
              key={membership.tenantId}
              variant="outline"
              onPress={() => onSelect(membership.tenantId)}
              accessibilityLabel={`Manage ${membership.legalName}`}
            >
              <Text>{membership.legalName}</Text>
            </Button>
          ))}
        </View>
      </View>
    </Screen>
  );
}
