import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { toast } from "sonner";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ConcertDraftCreatedPayload } from "@concertable/web/features/notifications/types";

export function useVenueNotifications(session: TenantSession | undefined) {
  const router = useRouter();

  useEffect(() => {
    if (session === undefined) return;
    notificationConnection.on(
      "ConcertDraftCreated",
      (payload: ConcertDraftCreatedPayload) => {
        if (!tenantSession.isCurrent(session)) return;
        toast.success("Your concert has been created");
        void router.navigate({
          to: "/my/concerts/concert/$id",
          params: { id: payload },
        });
      },
    );

    return () => {
      notificationConnection.off("ConcertDraftCreated");
    };
  }, [router, session]);
}
