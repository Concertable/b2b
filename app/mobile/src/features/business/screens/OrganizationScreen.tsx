import { useQuery } from "@tanstack/react-query";
import { ScrollView, View } from "react-native";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import { Text } from "@concertable/mobile/components/ui/text";
import { businessApi } from "../api/businessApi";

export function OrganizationScreen() {
  const organization = useQuery({
    queryKey: currentPrivateQueryKey("organization"),
    queryFn: businessApi.getOrganization,
  });
  const members = useQuery({
    queryKey: currentPrivateQueryKey("members"),
    queryFn: businessApi.getMembers,
  });

  return (
    <ScrollView className="flex-1 bg-background px-5 py-6">
      <Text className="text-2xl font-semibold">Organization</Text>
      {organization.data ? (
        <View className="mt-5 gap-2 rounded-lg border border-border p-4">
          <Text className="text-lg font-semibold">
            {organization.data.legalName}
          </Text>
          <Text>{organization.data.contactEmail}</Text>
          <Text className="text-muted-foreground">
            {organization.data.businessActivities.length === 0
              ? "No marketplace activities active"
              : organization.data.businessActivities.join(", ")}
          </Text>
        </View>
      ) : null}
      <Text className="mt-8 text-lg font-semibold">Members</Text>
      <View className="mt-3 gap-3">
        {members.data?.map((member) => (
          <View
            className="rounded-lg border border-border p-4"
            key={member.userId}
          >
            <Text>{member.email}</Text>
            <Text className="capitalize text-muted-foreground">
              {member.role}
            </Text>
          </View>
        ))}
      </View>
      {organization.isError || members.isError ? (
        <Text className="mt-4 text-destructive">
          Failed to load organization settings.
        </Text>
      ) : null}
    </ScrollView>
  );
}
