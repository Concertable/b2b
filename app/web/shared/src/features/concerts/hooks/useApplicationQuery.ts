import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";
import type { ESignatureRequest } from "@concertable/shared/features/concerts/types";
import applicationApi from "../api/applicationApi";
import type { ApplicationProposal } from "../types";

export function useApplicationsByOpportunityQuery(opportunityId: number) {
  return useQuery({
    queryKey: currentPrivateQueryKey("applications", "opportunity", opportunityId),
    queryFn: () => applicationApi.getApplicationsByOpportunityId(opportunityId),
  });
}

export function useApplicationQuery(applicationId: number) {
  return useQuery({
    queryKey: currentPrivateQueryKey("applications", applicationId),
    queryFn: () => applicationApi.getProposal(applicationId),
  });
}

export function useAcceptCheckoutQuery(applicationId: number) {
  return useQuery({
    queryKey: currentPrivateQueryKey("applications", applicationId, "checkout"),
    queryFn: () => applicationApi.acceptCheckout(applicationId),
  });
}

export function useApplyCheckoutQuery(opportunityId: number) {
  return useQuery({
    queryKey: currentPrivateQueryKey("opportunities", opportunityId, "apply-checkout"),
    queryFn: () => applicationApi.applyCheckout(opportunityId),
  });
}

export function useAcceptApplicationMutation(opportunityId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      applicationId,
      eSignature,
    }: {
      applicationId: number;
      eSignature: ESignatureRequest;
    }) => applicationApi.acceptApplication(applicationId, eSignature),
    onSuccess: (_data, { applicationId }) => {
      queryClient.setQueryData<ApplicationProposal>(
        currentPrivateQueryKey("applications", applicationId),
        (application) =>
          application ? { ...application, status: "accepted" } : application,
      );
      queryClient.invalidateQueries({
        queryKey: currentPrivateQueryKey("applications", "opportunity", opportunityId),
      });
    },
  });
}

export function usePendingApplicationsQuery() {
  return useQuery({
    queryKey: currentPrivateQueryKey("applications", "artist", "pending"),
    queryFn: () => applicationApi.getPendingForArtist(),
  });
}

export function useRecentDeniedApplicationsQuery() {
  return useQuery({
    queryKey: currentPrivateQueryKey("applications", "artist", "recently-denied"),
    queryFn: () => applicationApi.getRecentDeniedForArtist(),
  });
}

export function useWithdrawApplicationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (applicationId: number) =>
      applicationApi.withdrawApplication(applicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: currentPrivateQueryKey("applications"),
      });
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
        queryKey: currentPrivateQueryKey("applications", "opportunity", opportunityId),
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
        queryKey: currentPrivateQueryKey("applications", "opportunity", opportunityId),
      });
    },
  });
}
