import { useMutation, useQueryClient } from "@tanstack/react-query";
import artistApi from "../api/artistApi";
import { artistKeys } from "./useArtistQuery";

export function useCreateArtistMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: artistApi.createArtist,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: artistKeys.my() }),
  });
}

export function useUpdateArtistMutation(tenantId?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: artistApi.updateArtist,
    onSuccess: (artist) =>
      queryClient.setQueryData(artistKeys.myForTenant(tenantId), artist),
  });
}
