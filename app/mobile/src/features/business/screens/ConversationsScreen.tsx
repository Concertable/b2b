import { useQuery } from "@tanstack/react-query";
import { ScrollView, View } from "react-native";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import { Text } from "@concertable/mobile/components/ui/text";
import { businessApi } from "../api/businessApi";

export function ConversationsScreen() {
  const conversations = useQuery({
    queryKey: currentPrivateQueryKey("conversations", "previews"),
    queryFn: businessApi.getConversations,
  });

  return (
    <ScrollView className="flex-1 bg-background px-5 py-6">
      <Text className="text-2xl font-semibold">Messages</Text>
      {conversations.isLoading ? (
        <Text className="mt-4 text-muted-foreground">Loading messages…</Text>
      ) : null}
      {conversations.isError ? (
        <Text className="mt-4 text-destructive">Failed to load messages.</Text>
      ) : null}
      {conversations.data?.length === 0 ? (
        <Text className="mt-4 text-muted-foreground">No conversations yet.</Text>
      ) : null}
      <View className="mt-4 gap-3">
        {conversations.data?.map((conversation) => (
          <View
            className="rounded-lg border border-border p-4"
            key={conversation.conversationId}
          >
            <Text className="font-semibold">
              {conversation.participants.map((item) => item.displayName).join(", ")}
            </Text>
            <Text className="mt-1 text-muted-foreground">
              {conversation.preview}
            </Text>
          </View>
        ))}
      </View>
    </ScrollView>
  );
}
