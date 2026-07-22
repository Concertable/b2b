import { apiClient } from "@concertable/shared/lib/apiClient";
import type { Membership } from "@b2b/features/tenant";

const invitationApi = {
  accept: async (id: string): Promise<Membership> => {
    const { data } = await apiClient.post<Membership>(`/invitation/${id}/accept`);
    return data;
  },
};

export default invitationApi;
