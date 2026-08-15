export interface MessagePreview {
  id: number;
  otherPartyName: string;
  otherPartyAvatarUrl?: string;
  preview: string;
  at: string;
  unread: boolean;
  href: string;
}
