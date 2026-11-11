import { useMutation } from "@tanstack/react-query";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import conversationApi from "../api/conversationApi";
import { requireCurrentSession } from "../queryOptions";
import type { ReportMessageRequest, SelectedMessage } from "../types";

export function useReportMessageMutation(
  session: TenantSession,
  selected: SelectedMessage,
) {
  return useMutation({
    meta: { silenceErrors: true },
    mutationFn: (request: ReportMessageRequest) => {
      requireCurrentSession(session);
      return conversationApi.reportMessage(
        selected.conversationId,
        selected.messageId,
        request,
      );
    },
  });
}
