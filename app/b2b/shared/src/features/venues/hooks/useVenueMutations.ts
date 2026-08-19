import { useMutation, useQueryClient } from "@tanstack/react-query";
import venueApi from "../api/venueApi";
import { venueKeys } from "./useVenueQuery";

export function useCreateVenueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: venueApi.createVenue,
    onSuccess: (venue) => queryClient.setQueryData(venueKeys.details(), venue),
  });
}

export function useUpdateVenueMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: venueApi.updateVenue,
    onSuccess: (venue) => queryClient.setQueryData(venueKeys.details(), venue),
  });
}
