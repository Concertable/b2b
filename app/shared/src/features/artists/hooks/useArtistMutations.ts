import { useMutation, useQueryClient } from "@tanstack/react-query";
import artistApi from "../api/artistApi";
import { artistKeys } from "./useArtistQuery";

export function useCreateArtistMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: artistApi.createArtist,
    onSuccess: (artist) =>
      queryClient.setQueryData(artistKeys.my(), artist),
  });
}

export function useUpdateArtistMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: artistApi.updateArtist,
    onSuccess: (artist) =>
      queryClient.setQueryData(artistKeys.my(), artist),
  });
}
