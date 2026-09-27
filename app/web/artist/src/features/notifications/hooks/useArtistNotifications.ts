import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ApplicationAcceptedPayload } from "@concertable/web/features/notifications/types";

export function useArtistNotifications(session: TenantSession | undefined) {
  const router = useRouter();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (session === undefined) return;
    notificationConnection.on("MessageReceived", () => {
      if (!tenantSession.isCurrent(session)) return;
      void queryClient.invalidateQueries({ queryKey: ["messages"] });
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
      notificationConnection.off("MessageReceived");
      notificationConnection.off("ApplicationAccepted");
    };
  }, [queryClient, router, session]);
}
