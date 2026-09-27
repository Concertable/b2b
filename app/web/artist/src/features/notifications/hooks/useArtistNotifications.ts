import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ApplicationAcceptedPayload } from "@concertable/web/features/notifications/types";

export function useArtistNotifications(session: TenantSession | undefined) {
  const router = useRouter();

  useEffect(() => {
    if (session === undefined) return;
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
      notificationConnection.off("ApplicationAccepted");
    };
  }, [router, session]);
}
