import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { actionLinkApi } from "@concertable/b2b/features/concerts";
import type { ApplicationActionName } from "../applicationActions";
import type { Application } from "../types";

export function useArtistApplicationActions() {
  const queryClient = useQueryClient();
  const [withdrawal, setWithdrawal] = useState<Application>();
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
      if (name === "withdraw") {
        toast.success("Application withdrawn.");
        void queryClient.invalidateQueries({
          queryKey: ["dashboard", "artist"],
        });
        void queryClient.invalidateQueries({ queryKey: ["applications"] });
      }
      setWithdrawal(undefined);
    },
  });

  function request(name: ApplicationActionName, application: Application) {
    if (name === "withdraw") {
      setWithdrawal(application);
      return;
    }
    mutation.mutate({ name, application });
  }

  return {
    request,
    isOpen: withdrawal !== undefined,
    dismiss: () => setWithdrawal(undefined),
    confirm: () => {
      if (withdrawal !== undefined) {
        mutation.mutate({ name: "withdraw", application: withdrawal });
      }
    },
    isPending: mutation.isPending,
  };
}
