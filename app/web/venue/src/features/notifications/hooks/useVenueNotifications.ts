import { useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import { notificationConnection } from "@/lib/signalr";
import type { ConcertDraftCreatedPayload } from "@/features/notifications";

export function useVenueNotifications() {
  const router = useRouter();
  const queryClient = useQueryClient();

  useMountEffect(() => {
    notificationConnection.on("MessageReceived", () => {
      void queryClient.invalidateQueries({ queryKey: ["messages"] });
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
      notificationConnection.off("MessageReceived");
      notificationConnection.off("ConcertDraftCreated");
    };
  });
}
