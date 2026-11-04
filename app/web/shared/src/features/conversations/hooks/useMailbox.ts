import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import { conversationQueries } from "../queryOptions";
import { getReadPosition } from "../readPositions";
import type { ConversationReadPosition } from "../types";
import { useAdvanceReadPositionsMutation } from "./useAdvanceReadPositionsMutation";
import { useConversationPreviewsQuery } from "./useConversationPreviewsQuery";
import { useConversationUnreadCountQuery } from "./useConversationUnreadCountQuery";

export function useMailbox(session: TenantSession) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const openRef = useRef(false);
  const operation = useRef(0);
  const previews = useConversationPreviewsQuery(session, pageNumber, open);
  const unread = useConversationUnreadCountQuery(session);
  const advance = useAdvanceReadPositionsMutation(session);

  useEffect(
    () => () => {
      openRef.current = false;
      operation.current += 1;
    },
    [],
  );

  const loadPage = async (nextPage: number) => {
    if (!openRef.current || !tenantSession.isCurrent(session)) return;
    const ownOperation = ++operation.current;
    const ownsOpen = () =>
      openRef.current &&
      ownOperation === operation.current &&
      tenantSession.isCurrent(session);
    setPageNumber(nextPage);
    advance.reset();
    try {
      const conversations = await queryClient.fetchQuery({
        ...conversationQueries.previews(session, nextPage),
        staleTime: 0,
      });
      if (!ownsOpen()) return;
      const outcomes = await Promise.allSettled(
        conversations.map(async (conversation) => {
          const messages = await queryClient.fetchQuery({
            ...conversationQueries.messages(session, conversation.conversationId),
            staleTime: 0,
          });
          return getReadPosition(conversation.conversationId, messages);
        }),
      );
      if (!ownsOpen()) return;
      const positions = outcomes.flatMap(
        (outcome): ConversationReadPosition[] =>
          outcome.status === "fulfilled" && outcome.value !== undefined
            ? [outcome.value]
            : [],
      );
      if (positions.length > 0) await advance.mutateAsync(positions);
    } catch {
      return;
    }
  };

  const changeOpen = (next: boolean) => {
    if (next === openRef.current) return;
    openRef.current = next;
    operation.current += 1;
    setOpen(next);
    if (next) void loadPage(1);
  };

  return {
    open,
    changeOpen,
    pageNumber,
    conversations: previews.data ?? [],
    unreadCount: unread.data,
    isLoading: open && previews.isPending,
    isError: previews.isError,
    unreadFailed: unread.isError,
    readFailed: advance.isError,
    isReading: advance.isPending,
    refresh: () => {
      void loadPage(1);
    },
    previous: () => {
      void loadPage(Math.max(1, pageNumber - 1));
    },
    next: () => {
      void loadPage(pageNumber + 1);
    },
  };
}
