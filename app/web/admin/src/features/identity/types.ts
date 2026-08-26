import type { User } from "@concertable/web/features/auth/types";

export interface Identity extends User {
  readonly isAdmin: boolean;
}
