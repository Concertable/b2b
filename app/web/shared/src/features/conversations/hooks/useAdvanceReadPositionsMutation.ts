import { useMutation, useQueryClient } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import conversationApi from "../api/conversationApi";
import { conversationKeys } from "../queryKeys";
import { requireCurrentSession } from "../queryOptions";
import type { ConversationReadPosition } from "../types";

export function useAdvanceReadPositionsMutation(session: TenantSession) {
  const queryClient = useQueryClient();
  return useMutation({
    meta: { silenceErrors: true },
    mutationFn: async (positions: readonly ConversationReadPosition[]) => {
      const outcomes = await Promise.allSettled(
        positions.map(async (position) => {
          requireCurrentSession(session);
          await conversationApi.advanceReadPosition(position.conversationId, {
            throughSequence: position.throughSequence,
          });
        }),
      );
      const failures = outcomes.filter(
        (outcome): outcome is PromiseRejectedResult =>
          outcome.status === "rejected",
      );
      if (failures.length > 0)
        throw new AggregateError(
          failures.map((failure) => failure.reason),
          "Read position update failed.",
        );
    },
    onSettled: async () => {
      if (!tenantSession.isCurrent(session)) return;
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: conversationKeys.unreadCount(session),
        }),
        queryClient.invalidateQueries({
          queryKey: conversationKeys.previews(session),
        }),
      ]);
    },
  });
}
