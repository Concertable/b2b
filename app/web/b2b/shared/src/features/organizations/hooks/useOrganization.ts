import { toast } from "sonner";
import { useOrganizationQuery } from "./useOrganizationQuery";
import { useUpdateOrganizationMutation } from "./useUpdateOrganizationMutation";
import { updateOrganizationRequestSchema } from "../schemas/updateOrganizationRequestSchema";
import { OrganizationBuffer } from "../types";

export function useOrganization() {
  const { data: organization, isLoading } = useOrganizationQuery();
  const { mutate, isPending } = useUpdateOrganizationMutation();

  const save = (buffer: OrganizationBuffer) => {
    const parsed = updateOrganizationRequestSchema.safeParse(buffer);
    if (parsed.success)
      mutate(OrganizationBuffer.toUpdateRequest(parsed.data), {
        onSuccess: () => toast.success("Details saved"),
      });
    return parsed;
  };

  return { organization, isLoading, isSaving: isPending, save };
}
