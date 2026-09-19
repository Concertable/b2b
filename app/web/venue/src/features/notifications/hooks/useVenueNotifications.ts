import { useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ConcertDraftCreatedPayload } from "@concertable/web/features/notifications/types";

export function useVenueNotifications() {
  const router = useRouter();
  const queryClient = useQueryClient();

  useMountEffect(() => {
    notificationConnection.on("ConversationChanged", () => {
      void queryClient.invalidateQueries({ queryKey: ["dashboard", "venue", "inbox"] });
    });

    notificationConnection.on(
      "ConcertDraftCreated",
      (payload: ConcertDraftCreatedPayload) => {
        toast.success("Your concert has been created");
        void router.navigate({
          to: "/my/concerts/concert/$id",
          params: { id: payload },
        });
      },
    );

    return () => {
      notificationConnection.off("ConversationChanged");
      notificationConnection.off("ConcertDraftCreated");
    };
  });
}
