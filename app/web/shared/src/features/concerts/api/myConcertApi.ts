import { apiClient } from "@concertable/shared/lib/apiClient";
import type { MyConcert } from "../types";

// Owner (manager) read of a concert the caller is a party to: GET /concert/user/{id}.
// B2B-only — it is tenant-scoped (404 for non-parties) and carries the party action links
// (contract download, cancel) the public marketplace read omits, so it must not live in the
// cross-platform @concertable/shared core.
const myConcertApi = {
  getMyConcert: async (id: number): Promise<MyConcert> => {
    const { data } = await apiClient.get<MyConcert>(`/concert/user/${id}`);
    return data;
  },
};

export default myConcertApi;
