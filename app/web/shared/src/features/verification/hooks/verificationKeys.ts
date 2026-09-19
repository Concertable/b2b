import { currentPrivateQueryKey } from "@concertable/b2b/features/tenant";

export const verificationKeys = {
  status: () => currentPrivateQueryKey("verification"),
};
