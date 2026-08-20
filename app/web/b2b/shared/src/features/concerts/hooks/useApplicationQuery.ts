import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import applicationApi from "../api/applicationApi";
import type { Application } from "../types";

export function useApplicationsByOpportunityQuery(opportunityId: number) {
  return useQuery({
    queryKey: ["applications", "opportunity", opportunityId],
    queryFn: () => applicationApi.getApplicationsByOpportunityId(opportunityId),
  });
}

export function useApplicationQuery(applicationId: number) {
  return useQuery({
    queryKey: ["applications", applicationId],
    queryFn: () => applicationApi.getApplicationById(applicationId),
  });
}

export function useAcceptCheckoutQuery(applicationId: number) {
  return useQuery({
    queryKey: ["applications", applicationId, "checkout"],
    queryFn: () => applicationApi.acceptCheckout(applicationId),
  });
}

export function useApplyCheckoutQuery(opportunityId: number) {
  return useQuery({
    queryKey: ["opportunities", opportunityId, "apply-checkout"],
    queryFn: () => applicationApi.applyCheckout(opportunityId),
  });
}

export function useAcceptApplicationMutation(opportunityId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      applicationId,
      eSignature,
      body,
    }: {
      applicationId: number;
      eSignature: ESignatureRequest;
      body?: { paymentMethodId: string };
    }) => applicationApi.acceptApplication(applicationId, eSignature, body),
    onSuccess: (_data, { applicationId }) => {
      queryClient.setQueryData<Application>(
        ["applications", applicationId],
        (application) =>
          application ? { ...application, status: "accepted" } : application,
      );
      queryClient.invalidateQueries({
        queryKey: ["applications", "opportunity", opportunityId],
      });
    },
  });
}

export function usePendingApplicationsQuery() {
  return useQuery({
    queryKey: ["applications", "artist", "pending"],
    queryFn: () => applicationApi.getPendingForArtist(),
  });
}

export function useRecentDeniedApplicationsQuery() {
  return useQuery({
    queryKey: ["applications", "artist", "recently-denied"],
    queryFn: () => applicationApi.getRecentDeniedForArtist(),
  });
}

export function useWithdrawApplicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (applicationId: number) =>
      applicationApi.withdrawApplication(applicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["applications"] });
    },
  });
}

export function useRejectApplicationMutation(opportunityId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (applicationId: number) =>
      applicationApi.rejectApplication(applicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["applications", "opportunity", opportunityId],
      });
    },
  });
}

export function useCancelApplicationMutation(opportunityId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (applicationId: number) =>
      applicationApi.cancelApplication(applicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["applications", "opportunity", opportunityId],
      });
    },
  });
}
