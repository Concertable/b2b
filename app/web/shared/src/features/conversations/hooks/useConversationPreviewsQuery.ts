import { useQuery } from "@tanstack/react-query";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { conversationQueries } from "../queryOptions";

export function useConversationPreviewsQuery(
  session: TenantSession | undefined,
  pageNumber = 1,
  enabled = true,
) {
  return useQuery({
    ...conversationQueries.previews(session, pageNumber),
    enabled: enabled && session !== undefined,
  });
}
