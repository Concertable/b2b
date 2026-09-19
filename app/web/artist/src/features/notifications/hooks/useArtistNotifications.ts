import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { artistDashboardKey } from "../../dashboard/queryKeys";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ApplicationAcceptedPayload } from "@concertable/web/features/notifications/types";

export function useArtistNotifications(session: TenantSession | undefined) {
  const router = useRouter();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (session === undefined) return;
    notificationConnection.on("ConversationChanged", () => {
      if (!tenantSession.isCurrent(session)) return;
      void queryClient.invalidateQueries({
        queryKey: artistDashboardKey("inbox"),
      });
    });

    notificationConnection.on(
      "ApplicationAccepted",
      (payload: ApplicationAcceptedPayload) => {
        if (!tenantSession.isCurrent(session)) return;
        void router.navigate({
          to: "/my/concerts/concert/$id",
          params: { id: payload },
        });
      },
    );

    return () => {
      notificationConnection.off("ConversationChanged");
      notificationConnection.off("ApplicationAccepted");
    };
  }, [queryClient, router, session]);
}
