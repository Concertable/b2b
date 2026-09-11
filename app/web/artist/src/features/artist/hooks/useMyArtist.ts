import { useMyArtist as useMyArtistShared } from "@concertable/b2b/features/artists";
import { toast } from "sonner";
import { useTenant } from "@concertable/web-b2b/features/tenant/hooks/useTenant";

export function useMyArtist() {
  const { activeMembership } = useTenant("artist");

  return useMyArtistShared(activeMembership?.tenantId, {
    onSuccess: () => toast.success("Artist saved!"),
  });
}
