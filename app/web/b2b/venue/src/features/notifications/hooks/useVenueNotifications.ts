import { useEffect } from "react";
import { useRouter } from "@tanstack/react-router";
import { toast } from "sonner";
import { notificationConnection } from "@/lib/signalr";
import type {
  MessageReceivedPayload,
  ConcertDraftCreatedPayload,
} from "@/features/notifications";

export function useVenueNotifications() {
  const router = useRouter();

  useEffect(() => {
    console.log(
      "[SignalR] useVenueNotifications mounted, connection state:",
      notificationConnection.state,
    );

    notificationConnection.on(
      "MessageReceived",
      (payload: MessageReceivedPayload) => {
        console.log("[SignalR] MessageReceived:", payload);
      },
    );

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
      console.log("[SignalR] useVenueNotifications unmounted");
      notificationConnection.off("MessageReceived");
      notificationConnection.off("ConcertDraftCreated");
    };
  }, []);
}
