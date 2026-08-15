import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";
import { actionLinkApi } from "@concertable/b2b/features/concerts";
import type { ApplicationActionName } from "../applicationActions";
import type { Application } from "../types";

type DestructiveActionName = "decline" | "cancel";

interface PendingAction {
  name: DestructiveActionName;
  application: Application;
}

export function useVenueApplicationActions() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [pendingAction, setPendingAction] = useState<PendingAction>();
  const mutation = useMutation({
    mutationFn: async ({
      name,
      application,
    }: {
      name: ApplicationActionName;
      application: Application;
    }) => {
      const action = application.actions[name];
      if (action === undefined) return;
      if (name === "contract") {
        await actionLinkApi.download(action, `contract-${application.id}.pdf`);
        return;
      }
      await actionLinkApi.execute(action);
    },
    onSuccess: (_data, { name }) => {
      if (name !== "contract") {
        toast.success(
          name === "decline"
            ? "Application declined."
            : "Application cancelled.",
        );
        void queryClient.invalidateQueries({
          queryKey: ["dashboard", "venue"],
        });
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      setPendingAction(undefined);
    },
  });

  function request(name: ApplicationActionName, application: Application) {
    if (name === "accept" || name === "checkout") {
      void navigate({
        to:
          name === "checkout"
            ? "/applications/$applicationId/checkout"
            : "/applications/$applicationId/accept",
        params: { applicationId: application.id },
      });
      return;
    }
    if (name === "contract") {
      mutation.mutate({ name, application });
      return;
    }
    setPendingAction({ name, application });
  }

  const confirmation =
    pendingAction === undefined
      ? undefined
      : {
          title:
            pendingAction.name === "decline"
              ? "Decline this application?"
              : "Cancel this application?",
          description:
            pendingAction.name === "decline"
              ? "The artist will be notified that their application was declined."
              : "The application will be cancelled and any payment held will be refunded in full.",
          confirmLabel:
            pendingAction.name === "decline"
              ? "Decline application"
              : "Cancel application",
          pendingLabel:
            pendingAction.name === "decline" ? "Declining..." : "Cancelling...",
        };

  return {
    request,
    confirmation,
    dismiss: () => setPendingAction(undefined),
    confirm: () => {
      if (pendingAction !== undefined) mutation.mutate(pendingAction);
    },
    isPending: mutation.isPending,
  };
}
