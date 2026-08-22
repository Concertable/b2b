import { toast } from "sonner";
import { useOrganizationQuery } from "./useOrganizationQuery";
import { useUpdateOrganizationMutation } from "./useUpdateOrganizationMutation";
import type { UpdateOrganizationRequest } from "../types";

export function useOrganization() {
  const { data: organization, isLoading } = useOrganizationQuery();
  const { mutate, isPending } = useUpdateOrganizationMutation();

  const save = (request: UpdateOrganizationRequest) =>
    mutate(request, {
      onSuccess: () => toast.success("Details saved"),
    });

  return { organization, isLoading, isSaving: isPending, save };
}
