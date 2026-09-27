import { FlagIcon } from "lucide-react";
import { Button } from "@concertable/web/components/ui/button";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { useConversationMessagesQuery } from "../hooks/useConversationMessagesQuery";
import type { MessagePreview, SelectedMessage } from "../types";

export function MailboxConversation({
  session,
  conversation,
  onReport,
}: Readonly<{
  session: TenantSession;
  conversation: MessagePreview;
  onReport: (selected: SelectedMessage) => void;
}>) {
  const messages = useConversationMessagesQuery(session, conversation.conversationId);

  if (messages.isPending)
    return (
      <p role="status" className="text-muted-foreground px-3 py-2.5 text-sm">
        Loading conversation...
      </p>
    );
  if (messages.isError)
    return (
      <p role="alert" className="text-destructive px-3 py-2.5 text-sm">
        Conversation unavailable. Try Refresh.
      </p>
    );

  return (
    <section className="divide-border divide-y">
      {messages.data.map((message) => (
        <div
          key={message.id}
          data-testid="mailbox-message"
          className="space-y-1 px-3 py-2.5"
        >
          <span className="text-foreground truncate text-xs font-medium">
            {conversation.participants.find(
              (participant) => participant.tenantId === message.senderTenantId,
            )?.displayName ?? "Unknown business"}
          </span>
          <p className="text-sm">{message.content}</p>
          {message.actions.report != null && (
            <Button
              variant="ghost"
              size="sm"
              className="text-muted-foreground h-auto px-0 py-0 text-[11px]"
              data-testid="message-report-trigger"
              onClick={() =>
                onReport({
                  conversationId: message.conversationId,
                  messageId: message.id,
                })
              }
            >
              <FlagIcon className="size-3" />
              Report
            </Button>
          )}
        </div>
      ))}
    </section>
  );
}
