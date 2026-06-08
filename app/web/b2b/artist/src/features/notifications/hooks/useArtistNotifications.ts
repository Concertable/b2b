import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { notificationConnection } from "@/lib/signalr";
import type {
  MessageReceivedPayload,
  ApplicationAcceptedPayload,
} from "@/features/notifications";

export function useArtistNotifications() {
  const router = useRouter();

  useEffect(() => {
    notificationConnection.on(
      "MessageReceived",
      (payload: MessageReceivedPayload) => {
        console.log("[SignalR] MessageReceived:", payload);
      },
    );

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
  }, []);
}
