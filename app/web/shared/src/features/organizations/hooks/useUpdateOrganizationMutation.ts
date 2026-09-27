import { useMutation, useQueryClient } from "@tanstack/react-query";
import organizationApi from "../api/organizationApi";
import { organizationQueryKey } from "./useOrganizationQuery";

export function useUpdateOrganizationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: organizationApi.update,
    onSuccess: (organization) => {
      queryClient.setQueryData(organizationQueryKey(), organization);
    },
  });
}
