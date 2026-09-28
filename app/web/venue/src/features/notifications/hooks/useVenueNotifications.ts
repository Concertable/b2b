import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useRouter } from "@tanstack/react-router";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { toast } from "sonner";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ConcertDraftCreatedPayload } from "@concertable/web/features/notifications/types";

export function useVenueNotifications(session: TenantSession | undefined) {
  const router = useRouter();
  const queryClient = useQueryClient();

  useEffect(() => {
    if (session === undefined) return;
    notificationConnection.on("MessageReceived", () => {
      if (!tenantSession.isCurrent(session)) return;
      void queryClient.invalidateQueries({ queryKey: ["messages"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard", "venue", "inbox"] });
    });
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
      notificationConnection.off("MessageReceived");
      notificationConnection.off("ConcertDraftCreated");
    };
  }, [queryClient, router, session]);
}
