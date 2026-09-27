import { useConversationPreviewsQuery } from "@concertable/web-b2b/features/conversations";
import { useTenant } from "@concertable/web-b2b/features/tenant";

export function useVenueInboxQuery() {
  const { session, permissions } = useTenant("venueOperator");
  return useConversationPreviewsQuery(session, 1, permissions.has("messages.read"));
}
