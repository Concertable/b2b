import { useMutation, useQueryClient } from "@tanstack/react-query";
import organizationApi from "../api/organizationApi";

export function useUpdateOrganizationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: organizationApi.update,
    onSuccess: (organization) => {
      queryClient.setQueryData(["organization"], organization);
    },
  });
}
