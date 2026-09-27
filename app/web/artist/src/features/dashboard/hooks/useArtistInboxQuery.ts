import { useConversationPreviewsQuery } from "@concertable/web-b2b/features/conversations";
import { useTenant } from "@concertable/web-b2b/features/tenant";

export function useArtistInboxQuery() {
  const { session, permissions } = useTenant("artist");
  return useConversationPreviewsQuery(session, 1, permissions.has("messages.read"));
}
