import { useRouter } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import { notificationConnection } from "@concertable/web/lib/signalr";
import type { ApplicationAcceptedPayload } from "@concertable/web/features/notifications";

export function useArtistNotifications() {
  const router = useRouter();
  const queryClient = useQueryClient();

  useMountEffect(() => {
    notificationConnection.on("MessageReceived", () => {
      void queryClient.invalidateQueries({ queryKey: ["messages"] });
    });

    notificationConnection.on(
      "ApplicationAccepted",
      (payload: ApplicationAcceptedPayload) => {
        console.log("[SignalR] ApplicationAccepted:", payload);
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
  });
}
