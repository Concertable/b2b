import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { notificationConnection } from "@concertable/web/lib/signalr";
import { conversationKeys } from "../queryKeys";

export function useConversationNotifications(session: TenantSession) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const changed = (event: { conversationId: number }) => {
      if (!tenantSession.isCurrent(session)) return;
      void Promise.all([
        queryClient.invalidateQueries({
          queryKey: conversationKeys.unreadCount(session),
        }),
        queryClient.invalidateQueries({
          queryKey: conversationKeys.previews(session),
        }),
        queryClient.invalidateQueries({
          queryKey: conversationKeys.messages(session, event.conversationId),
        }),
      ]);
    };
    notificationConnection.on("ConversationChanged", changed);
    return () => notificationConnection.off("ConversationChanged", changed);
  }, [queryClient, session]);
}
