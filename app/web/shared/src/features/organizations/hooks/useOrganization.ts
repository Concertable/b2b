import { toast } from "sonner";
import { useOrganizationQuery } from "./useOrganizationQuery";
import { useUpdateOrganizationMutation } from "./useUpdateOrganizationMutation";
import { updateOrganizationRequestSchema } from "../schemas/updateOrganizationRequestSchema";
import type {
  OrganizationFormValues,
  UpdateOrganizationRequest,
} from "../types";

export type OrganizationBuffer = OrganizationFormValues;

export function useOrganization() {
  const { data: organization, isLoading } = useOrganizationQuery();
  const { mutate, isPending } = useUpdateOrganizationMutation();

  const save = (
    input: OrganizationBuffer | UpdateOrganizationRequest,
  ) => {
    if ("taxCompliance" in input) {
      mutate(input, {
        onSuccess: () => toast.success("Details saved"),
      });
      return;
    }

    const parsed = updateOrganizationRequestSchema.safeParse(input);
    if (parsed.success)
      mutate(parsed.data, {
        onSuccess: () => toast.success("Details saved"),
      });
    return parsed;
  };

  return { organization, isLoading, isSaving: isPending, save };
}
