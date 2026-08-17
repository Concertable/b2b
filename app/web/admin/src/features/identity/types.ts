import type { User } from "@concertable/web/features/auth";

export interface Identity extends User {
  readonly isAdmin: boolean;
}
