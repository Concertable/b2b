export interface Admin {
  sub: string;
  email: string;
}

export interface AdminInvitation {
  id: string;
  email: string;
  createdAt: string;
  expiresAt: string;
}

export interface AdminOverview {
  admins: Admin[];
  pendingInvitations: AdminInvitation[];
}
