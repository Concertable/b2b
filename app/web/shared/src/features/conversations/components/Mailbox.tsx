import { useState } from "react";
import { MailIcon } from "lucide-react";
import { Button } from "@concertable/web/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@concertable/web/components/ui/popover";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { useConversationNotifications } from "../hooks/useConversationNotifications";
import { useMailbox } from "../hooks/useMailbox";
import type { SelectedMessage } from "../types";
import { MailboxConversation } from "./MailboxConversation";
import { ReportMessageDialog } from "./ReportMessageDialog";

const PAGE_SIZE = 5;

export function Mailbox({ session }: Readonly<{ session: TenantSession }>) {
  const inbox = useMailbox(session);
  useConversationNotifications(session);
  const [selected, setSelected] = useState<SelectedMessage>();
  const unreadCount = inbox.unreadCount ?? 0;

  return (
    <>
      <Popover open={inbox.open} onOpenChange={inbox.changeOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="ghost"
            size="icon"
            className="relative"
            aria-label="Open inbox"
            data-testid="mailbox-trigger"
          >
            <MailIcon />
            {unreadCount > 0 && (
              <span
                data-testid="mailbox-unread"
                className="bg-primary text-primary-foreground absolute -top-0.5 -right-0.5 flex size-4 items-center justify-center rounded-full text-[10px] font-medium"
              >
                {unreadCount > 99 ? "99+" : unreadCount}
              </span>
            )}
          </Button>
        </PopoverTrigger>

        <PopoverContent align="end" className="w-80 p-0">
          <div className="bg-secondary border-border border-b px-3 py-2">
            <p className="text-secondary-foreground text-sm font-medium">Inbox</p>
          </div>

          <div className="space-y-1 px-3 pt-2 text-sm">
            {inbox.unreadFailed && (
              <p role="status" className="text-muted-foreground">
                Unread count is unavailable.
              </p>
            )}
            {inbox.isLoading && (
              <p role="status" className="text-muted-foreground">
                Loading messages...
              </p>
            )}
            {inbox.isError && (
              <p role="alert" className="text-destructive">
                Messages could not be loaded. Try Refresh.
              </p>
            )}
            {inbox.readFailed && (
              <p role="alert" className="text-destructive">
                Some messages could not be marked read. Try Refresh.
              </p>
            )}
            {!inbox.isLoading &&
              !inbox.isError &&
              inbox.conversations.length === 0 && (
                <p className="text-muted-foreground">No conversations on this page.</p>
              )}
          </div>

          {!inbox.isError && (
            <div className="divide-border max-h-96 divide-y overflow-y-auto">
              {inbox.conversations.map((conversation) => (
                <MailboxConversation
                  key={conversation.conversationId}
                  session={session}
                  conversation={conversation}
                  onReport={setSelected}
                />
              ))}
            </div>
          )}

          <div className="border-border flex gap-2 border-t px-3 py-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={inbox.refresh}
              disabled={inbox.isReading}
            >
              Refresh
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={inbox.previous}
              disabled={inbox.pageNumber === 1 || inbox.isReading}
            >
              Previous
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={inbox.next}
              disabled={inbox.conversations.length < PAGE_SIZE || inbox.isReading}
            >
              Next
            </Button>
          </div>
        </PopoverContent>
      </Popover>

      {selected !== undefined && (
        <ReportMessageDialog
          key={`${selected.conversationId}:${selected.messageId}`}
          session={session}
          selected={selected}
          onClose={() => setSelected(undefined)}
        />
      )}
    </>
  );
}
