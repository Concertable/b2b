import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";

export const concertKeys = {
  all: () => currentPrivateQueryKey("concert"),
  operations: (id: number) => currentPrivateQueryKey("concert", id, "operations"),
  finance: (id: number) => currentPrivateQueryKey("concert", id, "finance"),
};
