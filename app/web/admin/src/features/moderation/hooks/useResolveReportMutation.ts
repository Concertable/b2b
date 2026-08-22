import { useMutation, useQueryClient } from "@tanstack/react-query";
import moderationApi from "../api/moderationApi";
import { moderationKeys } from "./moderationKeys";
import type { ResolveReportRequest } from "../schemas/resolveReportRequestSchema";

interface ResolveReportVariables {
  id: number;
  request: ResolveReportRequest;
}

export function useResolveReportMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: ResolveReportVariables) =>
      moderationApi.resolveReport(id, request),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: moderationKeys.reports }),
  });
}
