import { useState } from "react";
import { skipToken, useQuery } from "@tanstack/react-query";
import { ScrollView, TextInput, View } from "react-native";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import { Button } from "@concertable/mobile/components/ui/button";
import { Text } from "@concertable/mobile/components/ui/text";
import { businessApi } from "../api/businessApi";

export function OperationsScreen() {
  const [concertId, setConcertId] = useState("");
  const [selectedConcertId, setSelectedConcertId] = useState<number>();
  const summary = useQuery({
    queryKey: currentPrivateQueryKey(
      "concert",
      selectedConcertId,
      "summary",
    ),
    queryFn:
      selectedConcertId === undefined
        ? skipToken
        : () => businessApi.getConcertSummary(selectedConcertId),
  });

  return (
    <ScrollView className="flex-1 bg-background px-5 py-6">
      <Text className="text-2xl font-semibold">Operations</Text>
      <Text className="mt-2 text-muted-foreground">
        Open any concert summary shared with this organization.
      </Text>
      <View className="mt-6 gap-3">
        <TextInput
          accessibilityLabel="Concert ID"
          className="rounded-md border border-border px-4 py-3 text-foreground"
          keyboardType="number-pad"
          onChangeText={setConcertId}
          placeholder="Concert ID"
          value={concertId}
        />
        <Button
          disabled={!/^\d+$/.test(concertId)}
          onPress={() => setSelectedConcertId(Number(concertId))}
        >
          <Text>Open summary</Text>
        </Button>
      </View>
      {summary.isError ? (
        <Text className="mt-6 text-destructive">
          That concert summary is not available to this organization.
        </Text>
      ) : null}
      {summary.data ? (
        <View className="mt-6 gap-2 rounded-lg border border-border p-4">
          <Text className="text-lg font-semibold">{summary.data.name}</Text>
          <Text>{summary.data.artistName}</Text>
          <Text>{summary.data.venueName}</Text>
          <Text className="text-muted-foreground">{summary.data.state}</Text>
        </View>
      ) : null}
    </ScrollView>
  );
}
