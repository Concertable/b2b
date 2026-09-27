# B2B header inbox after the Conversation cutover

Design only, 27 September 2026. Source inspected in this worktree at `e948f1505`, whose parent
is the brief's `3d32203cf`. The existing Party Foundation plan and progress ledger remain the
implementation owner. This document is the requested input to that owner's mailbox repair;
it neither starts another delivery workstream nor authorizes this session to edit source.

## Decision

Own `Mailbox` in `app/web/shared/src/features/conversations/`, exported through
`@concertable/web-b2b/features/conversations`, and use it in venue, artist and business.

Use the existing Conversation APIs, with a small paging extension to previews:

| Event | Read/write | Result used |
|---|---|---|
| Header mounts with a selected, permitted membership | `GET /api/conversations/unread-count` | This membership's badge |
| Inbox opens, changes page or explicitly refreshes | `GET /api/conversations/previews?pageNumber=1` | Five conversation IDs and their participant displays |
| A preview page loads | `GET /api/conversations/{conversationId}/messages` for each conversation | Actual messages, sender tenant IDs, sequences and report actions |
| That open operation delivers messages successfully | `PUT /api/conversations/{conversationId}/read-position` | Maximum delivered sequence for each successfully loaded conversation |
| A report is submitted | `POST /api/conversations/{conversationId}/messages/{messageId}/report` | Existing validated report request; 204 means submitted |

**Do not add sender, sequence or report fields to `MessagePreviewDto`.** A preview identifies a
conversation; the message endpoint already supplies the message identity, sequence and permission.
Resolve a message's sender display by matching `message.senderTenantId` to that conversation's
`participants[].tenantId`. Never select the first other participant or look up an Artist/Venue profile.

This choice matters for the supplied scenarios. Both test and dev seeders create an inbound artist
message at sequence 1 followed by the venue's outbound message at sequence 2. The existing preview
contains sequence 2's content. Adding fields to that one preview would still fail to expose the
artist message and its report action. Reading each selected conversation gives the header both rows.
The bounded fan-out is at most five message requests per preview page; those existing message
responses contain the conversation's whole visible history. This design does not claim that history
is paged or introduce a composer, participant editor or separate conversation screen.

The default preview request still returns the first five conversations, so the existing dashboard
and mobile preview surfaces retain their array contract. The mailbox provides Previous/Next to
reach older conversations rather than turning `.Take(5)` into an invisible inbox boundary. It never
walks unseen pages automatically to clear the badge. Opening clears the badge in the supplied
single-conversation scenario. In a larger inbox, unread messages in unopened pages, failed reads,
and later arrivals correctly remain counted. There is no honest blanket zero while preserving
the plan's delivered-sequence rule.

## Findings that affect the implementation

1. All three layout imports still select the published `/message/*` client. A 404 from those routes
   accounts for the empty/error inbox and absent reporting controls.
2. `MessageRepository.GetRecentPreviewsAsync` computes `Unread` from the last message alone. The
   venue therefore gets `Unread = false` even though its sequence-1 inbound message is unread.
   Correct the predicate to mean any unread inbound visible message in that conversation.
3. `TenantFactory.Create` seeds both display and legal name from the manager's email. Both
   Conversation seeders now project `Tenant.EffectiveDisplayName`; that is not "The Rockers".
   Set the canonical seeded artist tenant's name deliberately, as described below. Do not restore
   profile-derived production display names or weaken the scenarios' sender assertion.
4. The detached trigger is a separate lifetime symptom. Ordinary query failure/refetch does not
   require React to replace a button. Likely causes are a parent repeatedly removing the mailbox,
   a changing component type/key, or a tenant-selection/authentication feedback loop. The inspected
   package `AppLayout` additionally gates `messagingSlot` through `useMeQuery()` (`user &&
   messagingSlot`), while B2B already owns its tenant/identity gate. The inspected `Mailbox` itself
   is a module-level component with an unconditional trigger, so **the supplied Playwright log
   does not prove which ancestor caused the loop**. Nor does allocating `<Mailbox />` as a prop on
   each render, by itself, change the component type.

Use one module-level B2B component, a stable trigger for every query state, and a key that changes
only at an actual tenant-session boundary. Put it beside `TenantSwitcher` in `headerSlot`, removing
the second package-owned identity gate from this control's path. Do not resolve/select a tenant
inside the mailbox, derive its key from unread count, or invalidate the router after read writes.
The browser gate below must prove stable DOM identity across the open/read/refetch cycle. If it
still detaches, capture the ancestor changes in that run and repair the B2B owner before closing
the defect; replacing an endpoint is not evidence that the lifetime failure is fixed.

React's state-preservation rules support this distinction between a rerender and a changed tree
position/type/key: [Preserving and resetting state](https://react.dev/learn/preserving-and-resetting-state).

## Backend changes and retained contracts

Paths below are relative to `api/src/Modules/Conversations/` unless stated otherwise.

### DTO and mapper: retain the division of responsibility

`Conversations.Application/DTOs/MessagePreviewDtos.cs`, before **and after**:

```csharp
internal sealed record MessagePreviewDto(
    int Id,
    int ConversationId,
    IReadOnlyList<ConversationParticipant> Participants,
    string Preview,
    DateTime At,
    bool Unread,
    string Href);
```

Here and below `Conversations.Application` abbreviates the actual directory
`Concertable.B2B.Conversations.Application`; the same abbreviation applies to Api and Infrastructure.
Keep the internal repository projection `MessagePreview` unchanged too. No new backend DTO,
`Response`, `SenderDto`, or client view model is needed for this decision.

`Conversations.Api/Responses/ConversationResponses.cs` already provides the additional information
on a message. Retain this response, including the unrelated existing fields:

```csharp
internal sealed record MessageResponse(
    int Id,
    int ConversationId,
    long Sequence,
    Guid SenderTenantId,
    Guid SentByUserId,
    string Content,
    DateTime SentAt,
    MessageAction? Action,
    MessageActions Actions);

internal sealed record MessageActions(ActionLink? Report);
```

The existing service and API mapper remain the permission source. These are retained current
excerpts, not new policy to duplicate in previews or React:

```csharp
var messages = await messageRepository.GetByConversationIdAsync(conversationId, ct);
var activeTenantId = tenantContext.GetTenantId();
return messages.Select(message => ToDto(message, message.SenderTenantId != activeTenantId)).ToList();
```

```csharp
new MessageActions(message.CanReport
    ? new ActionLink(
        $"/api/conversations/{message.ConversationId}/messages/{message.Id}/report",
        HttpMethods.Post)
    : null)
```

The first excerpt runs only after `GetMessagesAsync` has checked the conversation through its
filtered repository. `MessageRepository` also excludes hidden messages. The POST still requires
`messages.read`, resolves the message through that filtered repository, checks that it belongs to
the route's conversation, and rejects own-tenant messages. The returned action is a UI affordance;
the server rechecks on submission. No token role claim, controller tenant argument or new report
permission is introduced.

### Preview paging and the unread predicate

`Conversations.Api/Controllers/ConversationsController.cs`, before:

```csharp
public async Task<ActionResult<IReadOnlyList<MessagePreviewDto>>> GetRecentPreviews(CancellationToken ct) =>
    Ok(await conversationService.GetRecentPreviewsAsync(ct));
```

After, keeping the current permission and route attributes and importing
`System.ComponentModel.DataAnnotations`:

```csharp
[HasPermission(TenantPermission.MessagesReadName)]
[HttpGet("previews")]
public async Task<ActionResult<IReadOnlyList<MessagePreviewDto>>> GetRecentPreviews(
    CancellationToken ct,
    [FromQuery, Range(1, int.MaxValue / 5)] int pageNumber = 1) =>
    Ok(await conversationService.GetRecentPreviewsAsync(pageNumber, ct));
```

Update `IConversationService` and `ConversationService` to accept that `int pageNumber` before
the cancellation token. Name the changed repository operation for its key and update its interface
and sole service caller together:

```csharp
Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsByTenantIdAsync(
    Guid tenantId,
    Guid membershipId,
    int pageNumber,
    CancellationToken ct = default);
```

```csharp
var previews = await messageRepository.GetRecentPreviewsByTenantIdAsync(
    actor.TenantId, actor.MembershipId, pageNumber, ct);
```

The service's existing participant enrichment and `InboxHref` mapping stay intact. The API has a
single preview route and response shape, with a page selector; there is no legacy adapter.

`Conversations.Infrastructure/Repositories/MessageRepository.cs`, current deciding lines:

```csharp
.OrderByDescending(message => message.SentAt)
.Take(5)
```

```csharp
message.SenderTenantId != tenantId
    && !context.ConversationReadPositions.Any(position =>
        position.ConversationId == message.ConversationId
        && position.TenantId == tenantId
        && position.MembershipId == membershipId
        && position.LastReadSequence >= message.Sequence)
```

Replace the query method with:

```csharp
public async Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsByTenantIdAsync(
    Guid tenantId,
    Guid membershipId,
    int pageNumber,
    CancellationToken ct = default)
{
    var visible = context.Messages.Where(NotHidden);
    var latestIds = visible
        .GroupBy(message => message.ConversationId)
        .Select(group => group.OrderByDescending(message => message.Sequence)
            .Select(message => message.Id).First());

    return await visible
        .AsNoTracking()
        .Where(message => latestIds.Contains(message.Id))
        .OrderByDescending(message => message.SentAt)
        .ThenByDescending(message => message.Id)
        .Skip((pageNumber - 1) * 5)
        .Take(5)
        .Select(message => new MessagePreview(
            message.Id,
            message.ConversationId,
            message.Content,
            message.SentAt,
            visible.Any(inbound =>
                inbound.ConversationId == message.ConversationId
                && inbound.SenderTenantId != tenantId
                && !context.ConversationReadPositions.Any(position =>
                    position.ConversationId == inbound.ConversationId
                    && position.TenantId == tenantId
                    && position.MembershipId == membershipId
                    && position.LastReadSequence >= inbound.Sequence))))
        .ToListAsync(ct);
}
```

The existing context filters continue to enforce the exact current membership, grant scope and
validity. Paging never operates over an unfiltered set. The tie-breaker gives deterministic pages
for an unchanged dataset. New activity can move conversations between pages; Refresh and reopening
return to page 1. A full five-row page enables Next; an empty next page offers Previous. Do not
invent a total count or an unbounded "fetch everything" request.

### Read positions: use the current implementation

No change to the request or service's membership/sequence validation is needed:

```csharp
public sealed record AdvanceConversationReadPositionRequest(long ThroughSequence);
```

The current PostgreSQL upsert, rather than the historical SQL Server example in section 4.7, is
the implementation to retain:

```sql
ON CONFLICT ("ConversationId", "MembershipId")
DO UPDATE SET "LastReadSequence" = GREATEST(
    conversations."ConversationReadPositions"."LastReadSequence",
    EXCLUDED."LastReadSequence");
```

The client sends neither a timestamp nor tenant/user/membership identity in the body. The service
selects the caller's current membership. Its existing positive/readable-sequence validation and
authority fence remain in force. A lower retry cannot regress the position; a later arrival has
a greater sequence and remains unread. Never use the preview ID as a sequence, and never assume
that the latest outbound message means there is nothing to acknowledge.

### Repair the sender fixture at its owner

At `api/src/Seed/Concertable.B2B.Seed.Infrastructure/Factories/TenantFactory.cs`, the current creation
and completed legal details both use `email`. Extend this existing seed factory with an optional
`string? displayName = null` parameter after `taxComplianceComplete`, and replace those lines:

```csharp
var name = displayName ?? email;
var tenant = TenantEntity.Create(name, email, userId, createdAt, TenantSeedIds.For(userId));
```

```csharp
tenant.UpdateLegalDetails(name, SeedTaxCompliance)
```

At `SeedState.cs`, replace the existing `Tenants = SeedUsers.Managers.Select(...)` construction:

```csharp
var mailboxSenderName = catalog.Artists.Single(artist => artist.UserId == ArtistManager1.Id).Name;
Tenants = SeedUsers.Managers
    .Select(manager => TenantFactory.Create(
        manager.Id,
        manager.Email,
        manager.Kind == ManagerKind.Venue
            ? TenantBusinessActivityKind.VenueOperator
            : TenantBusinessActivityKind.Artist,
        now,
        taxComplianceComplete: !bareTenantUserIds.Contains(manager.Id),
        displayName: manager.Id == ArtistManager1.Id ? mailboxSenderName : null))
    .ToList();
```

This is a deliberate synthetic tenant name equal to the scenario's sender name. The venue tenant
keeps its current name, which `GroupInboxSteps.ChooseOrganizationAsync` expects. Production
continues to use Tenant-owned display events. The test/dev Conversation projections already
consume `seedData.Tenants`; do not insert a different string directly into those projections.
Fresh fixtures must show the same name through both `/auth/me` membership data where applicable
and Conversation participant displays. No database migration is needed.

## Frontend ownership and contracts

All new web files below belong under `app/web/shared/src/features/conversations/`. Reuse the
published generic buttons, popovers, dialogs and select primitives; replace its messaging feature
usage. Export `Mailbox` and the shared preview query hook from the existing `index.ts`.

```text
api/conversationApi.ts
components/Mailbox.tsx
components/MailboxConversation.tsx
components/ReportMessageDialog.tsx
hooks/useMailbox.ts
hooks/useConversationPreviewsQuery.ts
hooks/useConversationMessagesQuery.ts
hooks/useConversationUnreadCountQuery.ts
hooks/useAdvanceReadPositionsMutation.ts
hooks/useConversationNotifications.ts
hooks/useReportMessageMutation.ts
queryKeys.ts
queryOptions.ts
readPositions.ts
schemas/reportMessageRequestSchema.ts
types.ts
```

`types.ts` keeps the current `ConversationParticipant` and `MessagePreview`. Add the consumed
message fields and write bodies; TypeScript reads are domain nouns without server suffixes:

```ts
import type { ActionLink } from "@concertable/shared/types/common";
import type { z } from "zod";
import type { reportMessageRequestSchema } from "./schemas/reportMessageRequestSchema";

export interface ConversationMessage {
  id: number;
  conversationId: number;
  sequence: number;
  senderTenantId: string;
  sentByUserId: string;
  content: string;
  sentAt: string;
  actions: { report?: ActionLink };
}

export interface AdvanceConversationReadPositionRequest {
  throughSequence: number;
}

export interface ConversationReadPosition extends AdvanceConversationReadPositionRequest {
  conversationId: number;
}

export interface SelectedMessage {
  conversationId: number;
  messageId: number;
}

export type ReportMessageRequest = z.output<typeof reportMessageRequestSchema>;
export type ReportMessageFormValues = z.input<typeof reportMessageRequestSchema>;
```

`ConversationReadPosition` is the list item's route identity plus its proposed read position;
only `AdvanceConversationReadPositionRequest` is serialized as the PUT body. `SelectedMessage`
is local dialog selection, not a cached copy of a server response. The unused action-label field
on the server response does not need a new header chip or client enumeration in this repair.

### API: replace the header's four old routes

Before: published `messageApi` calls `/message/user`, `/message/user/unread-count`,
`/message/mark-read` and `/message/{id}/report`. B2B's existing `conversationApi` only has
`getPreviews()`.

After, `api/conversationApi.ts`, importing its types from `../types`:

```ts
const conversationApi = {
  getPreviews: async (pageNumber = 1, signal?: AbortSignal): Promise<MessagePreview[]> => {
    const { data } = await apiClient.get<MessagePreview[]>("/conversations/previews", {
      params: { pageNumber },
      signal,
    });
    return data;
  },
  getUnreadCount: async (signal?: AbortSignal): Promise<number> => {
    const { data } = await apiClient.get<number>("/conversations/unread-count", { signal });
    return data;
  },
  getMessages: async (
    conversationId: number,
    signal?: AbortSignal,
  ): Promise<ConversationMessage[]> => {
    const { data } = await apiClient.get<ConversationMessage[]>(
      `/conversations/${conversationId}/messages`,
      { signal },
    );
    return data;
  },
  advanceReadPosition: async (
    conversationId: number,
    request: AdvanceConversationReadPositionRequest,
  ): Promise<void> => {
    await apiClient.put(`/conversations/${conversationId}/read-position`, request);
  },
  reportMessage: async (
    conversationId: number,
    messageId: number,
    request: ReportMessageRequest,
  ): Promise<void> => {
    await apiClient.post(
      `/conversations/${conversationId}/messages/${messageId}/report`,
      request,
    );
  },
};

export default conversationApi;
```

Use the existing `apiClient` imported from `@concertable/shared/lib/apiClient`, already configured
by `b2bClient.ts`. Its base URL includes `/api`; these methods deliberately pass `/conversations`.
The report action enables the button; the typed API method addresses the same two IDs as its
returned link. Do not prepend `/api` again, use the old message-only URL, or repurpose the generic
bodyless `actionLinkApi.execute` for a report with a body.

### Query keys, cancellation and the tenant boundary

Before: the published mailbox uses unscoped `["messages"]` keys. The dashboard previews currently
use separate `venueDashboardKey("inbox")` and `artistDashboardKey("inbox")` caches.

After, `queryKeys.ts`:

```ts
import { privateQueryKey } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";

const root = (session: TenantSession | undefined) => session === undefined
  ? ["tenant", "unselected", "unselected", 0, "conversations"] as const
  : privateQueryKey(session, "conversations");

export const conversationKeys = {
  all: root,
  unreadCount: (session: TenantSession | undefined) => [...root(session), "unread-count"] as const,
  previews: (session: TenantSession | undefined) => [...root(session), "previews"] as const,
  previewPage: (session: TenantSession | undefined, pageNumber: number) =>
    [...root(session), "previews", pageNumber] as const,
  messages: (session: TenantSession | undefined, conversationId: number) =>
    [...root(session), "messages", conversationId] as const,
};
```

`queryOptions.ts` supplies shared fetch definitions, used by both observers and the open event.
The explicit session is captured when the operation is created. Keep the existing HTTP interceptor's
generation checks and the switch boundary's cancel/settle/remove sequence; do not add another
Axios client or a mutable global mailbox tenant.

```ts
import { queryOptions } from "@tanstack/react-query";
import { tenantSession } from "@concertable/b2b/features/tenant";
import type { TenantSession } from "@concertable/b2b/features/tenant/types";
import conversationApi from "./api/conversationApi";
import { conversationKeys } from "./queryKeys";

export function requireCurrentSession(session: TenantSession | undefined): void {
  if (session === undefined || !tenantSession.isCurrent(session))
    throw new Error("The organization changed. Reopen the inbox.");
}

export const conversationQueries = {
  previews: (session: TenantSession | undefined, pageNumber = 1) => queryOptions({
    queryKey: conversationKeys.previewPage(session, pageNumber),
    queryFn: ({ signal }) => {
      requireCurrentSession(session);
      return conversationApi.getPreviews(pageNumber, signal);
    },
    enabled: session !== undefined,
    meta: { silenceErrors: true },
  }),
  messages: (session: TenantSession | undefined, conversationId: number) => queryOptions({
    queryKey: conversationKeys.messages(session, conversationId),
    queryFn: ({ signal }) => {
      requireCurrentSession(session);
      return conversationApi.getMessages(conversationId, signal);
    },
    enabled: session !== undefined,
    meta: { silenceErrors: true },
  }),
  unreadCount: (session: TenantSession | undefined) => queryOptions({
    queryKey: conversationKeys.unreadCount(session),
    queryFn: ({ signal }) => {
      requireCurrentSession(session);
      return conversationApi.getUnreadCount(signal);
    },
    enabled: session !== undefined,
    refetchInterval: 30_000,
    meta: { silenceErrors: true },
  }),
};
```

Implement each raw hook in its named file; these are the complete deciding bodies:

```ts
export function useConversationPreviewsQuery(
  session: TenantSession | undefined,
  pageNumber = 1,
  enabled = true,
) {
  return useQuery({
    ...conversationQueries.previews(session, pageNumber),
    enabled: enabled && session !== undefined,
  });
}

export function useConversationMessagesQuery(session: TenantSession, conversationId: number) {
  return useQuery(conversationQueries.messages(session, conversationId));
}

export function useConversationUnreadCountQuery(session: TenantSession) {
  return useQuery(conversationQueries.unreadCount(session));
}
```

No `keepPreviousData` across pages or sessions. Old callbacks use their captured keys, never a
new `currentPrivateQueryKey()` lookup. Read queries forward TanStack Query's abort signal; the
existing `useTenant.selectTenant` waits for pending mutations before activating the new tenant.
This matters because cancelling a command's HTTP request does not prove that the server rolled it
back. [TanStack Query cancellation](https://tanstack.com/query/v5/docs/framework/react/guides/query-cancellation)
documents the query signal consumed here.

### Opening and acknowledging the delivered messages

`readPositions.ts` is pure logic. It uses the actual successful message response, not the preview's
`Unread` flag, ID, time, or a previously cached count:

```ts
export function getReadPosition(
  conversationId: number,
  messages: readonly ConversationMessage[],
): ConversationReadPosition | undefined {
  const throughSequence = messages.reduce(
    (highest, message) => message.conversationId === conversationId
      ? Math.max(highest, message.sequence)
      : highest,
    0,
  );
  return throughSequence === 0 ? undefined : { conversationId, throughSequence };
}
```

`hooks/useAdvanceReadPositionsMutation.ts` binds the session. Successful writes stand when another
conversation fails; refetch the count even after partial failure, then show a retry affordance.

```ts
export function useAdvanceReadPositionsMutation(session: TenantSession) {
  const queryClient = useQueryClient();
  return useMutation({
    meta: { silenceErrors: true },
    mutationFn: async (positions: readonly ConversationReadPosition[]) => {
      const outcomes = await Promise.allSettled(positions.map(async (position) => {
        requireCurrentSession(session);
        await conversationApi.advanceReadPosition(position.conversationId, {
          throughSequence: position.throughSequence,
        });
      }));
      const failures = outcomes.filter((outcome) => outcome.status === "rejected");
      if (failures.length > 0)
        throw new AggregateError(failures.map((failure) => failure.reason), "Read position update failed.");
    },
    onSettled: async () => {
      if (!tenantSession.isCurrent(session)) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: conversationKeys.unreadCount(session) }),
        queryClient.invalidateQueries({ queryKey: conversationKeys.previews(session) }),
      ]);
    },
  });
}
```

Do not set the count to zero optimistically or mutate a colleague's cache. The authoritative
refetch decides the badge. If a message was hidden between GET and PUT, the existing server may
reject that sequence: keep the failure visible and let Refresh retrieve the new visible tail.

`hooks/useMailbox.ts`, replacing the package's unconditional mark-inbox-read call on open:

```ts
export function useMailbox(session: TenantSession) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const openRef = useRef(false);
  const operation = useRef(0);
  const previews = useConversationPreviewsQuery(session, pageNumber, open);
  const unread = useConversationUnreadCountQuery(session);
  const advance = useAdvanceReadPositionsMutation(session);

  useEffect(() => () => {
    openRef.current = false;
    operation.current += 1;
  }, []);

  const loadPage = async (nextPage: number) => {
    if (!openRef.current || !tenantSession.isCurrent(session)) return;
    const ownOperation = ++operation.current;
    const ownsOpen = () => openRef.current
      && ownOperation === operation.current
      && tenantSession.isCurrent(session);
    setPageNumber(nextPage);
    advance.reset();
    try {
      const conversations = await queryClient.fetchQuery({
        ...conversationQueries.previews(session, nextPage),
        staleTime: 0,
      });
      if (!ownsOpen()) return;
      const outcomes = await Promise.allSettled(conversations.map(async (conversation) => {
        const messages = await queryClient.fetchQuery({
          ...conversationQueries.messages(session, conversation.conversationId),
          staleTime: 0,
        });
        return getReadPosition(conversation.conversationId, messages);
      }));
      if (!ownsOpen()) return;
      const positions = outcomes.flatMap((outcome) =>
        outcome.status === "fulfilled" && outcome.value !== undefined ? [outcome.value] : [],
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
    isLoading: previews.isPending,
    isError: previews.isError,
    unreadFailed: unread.isError,
    readFailed: advance.isError,
    isReading: advance.isPending,
    refresh: () => { void loadPage(1); },
    previous: () => { void loadPage(Math.max(1, pageNumber - 1)); },
    next: () => { void loadPage(pageNumber + 1); },
  };
}
```

The catch only ends this event's control flow: errors already live in their query/mutation state,
and the components render them inline. There is no second toast or success fabrication. The
effect only invalidates an in-flight UI operation on unmount; it does not fetch or mutate.
Both the visible message hooks and `fetchQuery` use the same keys, so concurrent reads deduplicate.
Acknowledgment follows successful delivery to the cache while this open/page operation still owns
the visible popover. Closing or changing page before delivery prevents new acknowledgments from
that operation. A PUT already dispatched may complete; the membership-scoped server result remains
valid and is reconciled by the count query.

Only opening, paging and Refresh acknowledge messages. SignalR refetches do not secretly advance
read state: arrivals while the popover remains open stay unread until Refresh or reopening. This
also prevents a PUT -> invalidate -> query-success -> PUT loop. The seed's sequence 2 advances
over the earlier inbound sequence 1 only for the opener's membership. A colleague still has 1.

### One ConversationChanged subscription

Add `hooks/useConversationNotifications.ts` and call it once from `Mailbox`, which is present in
all three permitted layouts. The existing connection startup/logout and tenant-switch stop/start
remain their current owners.

```ts
export function useConversationNotifications(session: TenantSession) {
  const queryClient = useQueryClient();
  useEffect(() => {
    const changed = (event: { conversationId: number }) => {
      if (!tenantSession.isCurrent(session)) return;
      void Promise.all([
        queryClient.invalidateQueries({ queryKey: conversationKeys.unreadCount(session) }),
        queryClient.invalidateQueries({ queryKey: conversationKeys.previews(session) }),
        queryClient.invalidateQueries({
          queryKey: conversationKeys.messages(session, event.conversationId),
        }),
      ]);
    };
    notificationConnection.on("ConversationChanged", changed);
    return () => notificationConnection.off("ConversationChanged", changed);
  }, [queryClient, session]);
}
```

Import `notificationConnection` from `@concertable/web/lib/signalr`. Remove only the
`ConversationChanged` registration and cleanup in `useVenueNotifications` and
`useArtistNotifications`; retain their ConcertDraftCreated/ApplicationAccepted behavior. Otherwise
their existing `.off("ConversationChanged")` would remove the new shared listener too. The
30-second count query is a recovery path for missed notifications/reconnects; the next open always
fetches fresh content. No message content or count is trusted from the event. Read-position PUTs
currently do not publish this event, so their local mutation invalidation is necessary.

Use the same preview key for the dashboard cards. Before each hook independently wraps
`conversationApi.getPreviews` with its dashboard key. Replace the two bodies:

```ts
export function useVenueInboxQuery() {
  const { session, permissions } = useTenant("venueOperator");
  return useConversationPreviewsQuery(session, 1, permissions.has("messages.read"));
}
```

```ts
export function useArtistInboxQuery() {
  const { session, permissions } = useTenant("artist");
  return useConversationPreviewsQuery(session, 1, permissions.has("messages.read"));
}
```

Import `useTenant` from the B2B web tenant feature and the preview hook from the B2B web
conversations feature. Keep the cards' array rendering. No mobile change is required by the
unchanged preview payload and default first page. The existing `/?inbox=open` preview-link
navigation is not the trigger used by these two scenarios; repairing dashboard navigation is
separate from this header-control repair and must not be claimed as completed here.

## Components and report form

`Mailbox.tsx` is a module-level component with a required session prop. Its trigger is always at
the same position for that session, independent of loading/error/empty/badge state. The report
dialog is its sibling, outside `PopoverContent`, so closing/unmounting the portal cannot discard
the selected report or its acknowledgment.

```tsx
export function Mailbox({ session }: Readonly<{ session: TenantSession }>) {
  const inbox = useMailbox(session);
  useConversationNotifications(session);
  const [selected, setSelected] = useState<SelectedMessage>();

  return <>
    <Popover open={inbox.open} onOpenChange={inbox.changeOpen}>
      <PopoverTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Open inbox" data-testid="mailbox-trigger">
          <MailIcon />
          {(inbox.unreadCount ?? 0) > 0 &&
            <span data-testid="mailbox-unread">{inbox.unreadCount}</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80 p-3">
        <p className="font-medium">Inbox</p>
        {inbox.unreadFailed && <p role="status">Unread count is unavailable.</p>}
        {inbox.isLoading && <p role="status">Loading messages…</p>}
        {inbox.isError && <p role="alert">Messages could not be loaded. Try Refresh.</p>}
        {inbox.readFailed && <p role="alert">Some messages could not be marked read. Try Refresh.</p>}
        {!inbox.isLoading && !inbox.isError && inbox.conversations.length === 0 &&
          <p>No conversations on this page.</p>}
        {!inbox.isError && <div className="max-h-96 overflow-y-auto">
          {inbox.conversations.map((conversation) =>
            <MailboxConversation
              key={conversation.conversationId}
              session={session}
              conversation={conversation}
              onReport={setSelected}
            />)}
        </div>}
        <div className="flex gap-2">
          <Button onClick={inbox.refresh} disabled={inbox.isReading}>Refresh</Button>
          <Button onClick={inbox.previous} disabled={inbox.pageNumber === 1 || inbox.isReading}>
            Previous
          </Button>
          <Button onClick={inbox.next} disabled={inbox.conversations.length < 5 || inbox.isReading}>
            Next
          </Button>
        </div>
      </PopoverContent>
    </Popover>
    {selected !== undefined && <ReportMessageDialog
      key={`${selected.conversationId}:${selected.messageId}`}
      session={session}
      selected={selected}
      onClose={() => setSelected(undefined)}
    />}
  </>;
}
```

The generic primitive imports are from `@concertable/web/components/ui/{button,popover}` and
the icon is from `lucide-react`. Preserve the current badge styling and 99+ presentation during
implementation if desired; they have no bearing on the request/identity mechanism.

`MailboxConversation.tsx` uses the exact participant match. A missing projection has the same
"Unknown business" fallback as the backend. Do not include every participant's name in each
`mailbox-message` row: doing so would make the sender locator match outbound rows too.

```tsx
export function MailboxConversation({ session, conversation, onReport }: Readonly<{
  session: TenantSession;
  conversation: MessagePreview;
  onReport: (selected: SelectedMessage) => void;
}>) {
  const messages = useConversationMessagesQuery(session, conversation.conversationId);
  if (messages.isPending) return <p role="status">Loading conversation…</p>;
  if (messages.isError) return <p role="alert">Conversation unavailable. Try Refresh.</p>;
  return <section>
    {messages.data.map((message) => <div key={message.id} data-testid="mailbox-message">
      <p className="font-medium">
        {conversation.participants.find((participant) =>
          participant.tenantId === message.senderTenantId)?.displayName ?? "Unknown business"}
      </p>
      <p>{message.content}</p>
      {message.actions.report != null && <Button
        data-testid="message-report-trigger"
        onClick={() => onReport({ conversationId: message.conversationId, messageId: message.id })}
      >Report</Button>}
    </div>)}
  </section>;
}
```

`schemas/reportMessageRequestSchema.ts` owns the existing report wire enum and 2,000-character
limit. Do not import the obsolete messaging feature to obtain its mutation, schema or form.

```ts
import { z } from "zod";

export const reportCategories = ["illegalContent", "harassment", "fraud", "spam", "other"] as const;
export const reportCategoryLabels = {
  illegalContent: "Illegal content",
  harassment: "Harassment",
  fraud: "Fraud",
  spam: "Spam",
  other: "Other",
} satisfies Record<(typeof reportCategories)[number], string>;

export const reportMessageRequestSchema = z.object({
  category: z.enum(reportCategories),
  details: z.string().trim().max(2000).transform((value) => value || undefined).optional(),
});
```

`hooks/useReportMessageMutation.ts` binds the selected IDs and session, leaving only editable
fields in mutation variables. Reporting does not advance read state or optimistically hide content.

```ts
export function useReportMessageMutation(session: TenantSession, selected: SelectedMessage) {
  return useMutation({
    meta: { silenceErrors: true },
    mutationFn: (request: ReportMessageRequest) => {
      requireCurrentSession(session);
      return conversationApi.reportMessage(selected.conversationId, selected.messageId, request);
    },
  });
}
```

`ReportMessageDialog.tsx` uses the same generic primitives and React Hook Form already used by
the package dialog. The mechanism below replaces its message-only hook and keeps the scenario's
test IDs. Import `Select`, `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem` from
`@concertable/web/components/ui/select` and the existing Button/Dialog/Label/Textarea primitives.

```tsx
export function ReportMessageDialog({ session, selected, onClose }: Readonly<{
  session: TenantSession;
  selected: SelectedMessage;
  onClose: () => void;
}>) {
  const messages = useConversationMessagesQuery(session, selected.conversationId);
  const message = messages.data?.find((item) => item.id === selected.messageId);
  const canReport = !messages.isError && message?.actions.report != null;
  const report = useReportMessageMutation(session, selected);
  const { control, register, handleSubmit, formState: { errors, isValid } } =
    useForm<ReportMessageFormValues, unknown, ReportMessageRequest>({
      resolver: zodResolver(reportMessageRequestSchema),
      defaultValues: { details: "" },
      mode: "onChange",
    });
  const close = () => { if (!report.isPending) onClose(); };
  const submit = (request: ReportMessageRequest) => {
    if (canReport && !report.isPending && tenantSession.isCurrent(session)) report.mutate(request);
  };

  return <Dialog open onOpenChange={(next) => { if (!next) close(); }}>
    <DialogContent
      showCloseButton={!report.isPending}
      onEscapeKeyDown={(event) => { if (report.isPending) event.preventDefault(); }}
      onInteractOutside={(event) => { if (report.isPending) event.preventDefault(); }}
    >
      <DialogHeader>
        <DialogTitle>Report this message</DialogTitle>
        <DialogDescription>Tell us what is wrong with this message.</DialogDescription>
      </DialogHeader>
      {report.isSuccess ? <p data-testid="report-confirmation">Your report has been submitted.</p> :
        <form id="report-message-form" onSubmit={handleSubmit(submit)}>
          <div data-testid="report-category">
            <Label htmlFor="report-category-input">Reason</Label>
            <Controller control={control} name="category" render={({ field }) =>
              <Select value={field.value ?? ""} onValueChange={field.onChange}>
                <SelectTrigger id="report-category-input"><SelectValue placeholder="Choose a reason" /></SelectTrigger>
                <SelectContent>{reportCategories.map((category) =>
                  <SelectItem key={category} value={category}>{reportCategoryLabels[category]}</SelectItem>
                )}</SelectContent>
              </Select>
            } />
            {errors.category && <p role="alert">Choose a reason.</p>}
          </div>
          <Label htmlFor="report-details">Details (optional)</Label>
          <Textarea id="report-details" data-testid="report-details" {...register("details")} />
          {errors.details && <p role="alert">{errors.details.message}</p>}
          {!canReport && <p role="alert">This message is no longer available to report.</p>}
          {report.isError && <p role="alert">The report could not be submitted. Please try again.</p>}
        </form>}
      <DialogFooter>
        <Button onClick={close} disabled={report.isPending}>{report.isSuccess ? "Close" : "Cancel"}</Button>
        {!report.isSuccess && <Button
          type="submit"
          form="report-message-form"
          data-testid="report-submit"
          disabled={!isValid || !canReport || report.isPending}
        >{report.isPending ? "Submitting…" : "Submit report"}</Button>}
      </DialogFooter>
    </DialogContent>
  </Dialog>;
}
```

The current backend persists a report even if its email notifier fails. Confirmation therefore
says submitted, not that an email definitely arrived. Errors remain beside the form, with no
duplicate global toast. Selecting another message creates a fresh form/mutation through its
stable selection key; a tenant-session change unmounts the whole mailbox and dialog.

## Exact layout changes

For all three files, remove:

```tsx
import { Mailbox } from "@concertable/web/features/messaging";
```

Add:

```tsx
import { Mailbox } from "@concertable/web-b2b/features/conversations";
```

Change `useTenant` destructuring to include `permissions` and `session`; retain the existing
selection chooser and app-specific notifications/route guards. Each layout computes the same
session identity from primitive values, not object identity:

```tsx
const mailbox = session !== undefined && permissions.has("messages.read")
  ? <Mailbox
      key={`${session.tenantId}:${session.membershipId}:${session.permissionVersion}:${session.generation}`}
      session={session}
    />
  : undefined;
```

This is an element value inside the layout, not a component function declared inside another
component. The component type is always the imported module-level `Mailbox`.

At `app/web/venue/src/routes/_venue/route.tsx`:

```tsx
const { selectionRequired, session, permissions } = useTenant("venueOperator");
```

```tsx
<AppLayout
  links={links}
  profileItems={profileItems}
  headerSlot={<><TenantSwitcher businessActivity="venueOperator" />{mailbox}</>}
/>
```

At `app/web/artist/src/routes/_artist/route.tsx`:

```tsx
const { selectionRequired, session, permissions } = useTenant("artist");
```

```tsx
<AppLayout
  links={links}
  profileItems={profileItems}
  headerSlot={<><TenantSwitcher businessActivity="artist" />{mailbox}</>}
/>
```

At `app/web/business/src/routes/_business/route.tsx`:

```tsx
const { selectionRequired, session, permissions } = useTenant();
```

```tsx
<AppLayout
  links={links}
  profileItems={links}
  headerSlot={<><TenantSwitcher />{mailbox}</>}
/>
```

Delete each `messagingSlot={<Mailbox />}` prop. Compute `mailbox` after the existing chooser
return. No wrapper may remove this trigger merely because its count or messages are fetching.
Business gains the same ConversationChanged subscription through the shared component without
a new role-specific notification hook. Header availability comes from the active membership's
server-provided `messages.read` permission, not profile kind or a token role.

## Tests and implementation acceptance

Use the existing Conversations integration project and existing Vitest Node suites. No frontend
test framework or component harness is required. DOM/lifecycle behavior belongs to the existing
Reqnroll/Playwright UI tier for this change.

| Owner | Required change/assertion |
|---|---|
| `MessagingInboxTests.Previews_UseParticipantListsAndFallBackToTheNewestVisibleMessage` | Change initial `Assert.False(preview.Unread)` to true: sequence 1 is still unread behind the outbound tail. Retain the hidden-tail fallback and read-position assertions. |
| `MessagingInboxTests` | Exercise first/default/second preview pages, deterministic tied-time ordering, no rows for an outsider, and 400 for invalid page numbers. A sixth unread conversation stays counted until its page is opened/read. |
| `MessagingInboxTests` | Owner and colleague fetch the same conversation; PUT through max delivered sequence for the owner returns 204 and count 0, while the colleague's tenant-selected client remains at 1. Repeat/lower PUT is harmless. Append an inbound message after GET and before PUT; it remains unread. |
| `MessagingInboxTests` | `participants` carries the canonical seeded artist tenant display. Match a message's actual sender ID, including a three-participant conversation; the display is not whichever participant is first. |
| `ContentReportApiTests` | Retain inbound-only action assertions and round-trip the returned action URL/method. Verify own-tenant, wrong-conversation/message pair, hidden and inaccessible messages cannot be reported. Existing anonymous, validation and mail assertions continue to pass. |
| Existing assignment/read-position tests | Retain membership-incarnation, assignment revocation, invalid/future sequence and provider-real monotonic-write coverage. Do not replace these with a mocked repository test. |
| `api/conversationApi.test.ts` | Update preview expectation for `{ params: { pageNumber }, signal }`; cover message path, count, PUT body and report path/body with both IDs. Reuse its existing HTTP-client mock boundary. |
| `queryKeys.test.ts` | Different tenant, membership or permission version never shares a key. Page/message suffixes nest under the invalidation prefixes. |
| `readPositions.test.ts` | Maximum delivered sequence, empty response, and another conversation's row cannot supply this conversation's read position. |
| `schemas/reportMessageRequestSchema.test.ts` | Required category, wire values, optional/blank details, 2,000 allowed and 2,001 rejected. |
| Existing tenant-session/HTTP tests | Keep cancellation, mutation settlement, captured header and stale-generation rejection green; add a mailbox scenario to the existing browser tier for switching with an open inbox/report. |
| `GroupInbox.feature` | The exact supplied scenario passes: artist sender visible, opener clears, colleague still has 1, colleague sees the same sender. |
| `ContentReport.feature` | The exact supplied scenario passes against the new POST and renders `report-confirmation` on 204. |

The membership integration test fits the existing class/helpers; its core after arranging the
two seed clients is:

```csharp
var tenantId = TenantSeedIds.For(fixture.SeedState.VenueManager1.Id);
var owner = fixture.CreateClient(fixture.SeedState.VenueManager1);
var colleague = fixture.CreateClient(fixture.SeedState.VenueManager3);
colleague.DefaultRequestHeaders.Add(TenantHeaders.TenantId, tenantId.ToString());
var conversationId = Assert.Single(await GetPreviewsAsync(owner)).ConversationId;
var delivered = await GetMessagesAsync(owner, conversationId);

await (await owner.PutAsync($"/api/conversations/{conversationId}/read-position",
        new { throughSequence = delivered.Max(message => message.Sequence) }))
    .ShouldBe(HttpStatusCode.NoContent);

Assert.Equal(0, await GetUnreadCountAsync(owner));
Assert.Equal(1, await GetUnreadCountAsync(colleague));
```

Keep all fixed IDs in `MailboxPage.cs`. Extend the existing UI verification to retain a handle to
the trigger before opening and assert it is still connected after the read-position response and
badge refresh. This checks the detachment regression rather than masking it with force-clicks,
sleeps or longer timeouts. Also verify one trigger, no `/message/*` requests, no row content under
the previous tenant after switching, and an available trigger while message loading fails.
Exercise the shared control in artist and activity-free business as well as venue; the server's
permission list controls visibility in each.

Validation commands for the implementing owner, from this worktree after its normal dependency
bootstrap:

```powershell
dotnet test api/src/Modules/Conversations/Tests/Concertable.B2B.Conversations.IntegrationTests/Concertable.B2B.Conversations.IntegrationTests.csproj
npm -w @concertable/web-b2b test
npm run build:web
```

The shared prebuild already runs its `vitest run src` tests. Require the existing CI and the UI
E2E workflow at the implementation's exact SHA. The Conversations integration run uses the real
PostgreSQL fixture, which also verifies translation of the new correlated unread predicate. Keep
the owning ledger's remote API/UI baseline rules; this design is no evidence that a failing
scenario now passes. No tests, builds or runtime reproduction were performed by this design session.

## Platform follow-up and scope closure

Do not edit `platform-frontend` in this P1 run or bump its packages merely to repair B2B. Its generic
primitives/layout are reusable; its old messaging endpoint/client/form is not the owner of B2B's
Conversation feature.

A platform cleanup follow-up is required: inventory remaining real consumers of the published
`@concertable/web/features/messaging` and `@concertable/shared/features/messaging` exports. Its
closure condition is removal of the obsolete `/message/*` feature and exports once no real
consumer needs them, publication of that package version, and affected consumers building against
it. If another product still serves that contract, move its behavior to that product's owner
before removal. There is no B2B fallback or endpoint alias to maintain. Record this handoff in
the existing Foundation ledger's external-owner section during implementation; this session is
limited to this one document. That cleanup is independent of this mailbox's delivery, which
continues using the already published generic primitives.

The implementation is complete when the shared mailbox is wired into all three layouts, the
two named UI scenarios and the focused gates pass, the DOM stability assertion holds, and no B2B
header request reaches `/message/*`. Source inspection and the snippets resolve the proposed
mechanisms; the exact ancestor responsible for the old detached DOM symptom remains an inference
until the implementing session supplies the browser evidence.

## Standards and design verification

Read for this design: root `AGENTS.md`, `CODE_PATTERNS.md`, Foundation sections 4.7/4.8 and its
current ledger, `app/web/shared/AGENTS.md`, the Conversations integration instructions and UI
E2E instructions. The web-shared AGENTS file contains older two-app paths/claims; actual package
exports and the three present layout consumers establish this feature's current B2B owner.

| Proposed paths | Standards consulted |
|---|---|
| This design | `base:plan-artifacts`, `engineering:plan-authoring`, `engineering:plans`, `engineering:docs-review` |
| Conversation controller/query/contracts | `dotnet:csharp-naming`, `dotnet:csharp-style`, `dotnet:http-api`, `dotnet:multitenancy` |
| Seed factory/state | `dotnet:seeding`; existing catalog and Tenant display ownership |
| Conversation client/hooks/components | `react:structure`, `react:server-state`, `react:http-layer`, `react:contract-naming`, `react:typescript-style`, `react:write-boundary`, `react:ui-components` |
| Tests | `dotnet:integration-testing`, `react:frontend-testing`, existing suite instructions |

The installed route lookup reported no readable `.agents/skill-routes.json` for this worktree;
standards were selected from the declared stack and target paths instead. No guidance files were
changed. The request limits output to this document, so no review ledger, commits, branch changes
or delivery operations are part of this design session. Snippets omit routine imports where their
owning modules are already named; they have been checked against the inspected source contracts,
not compiled as an implementation.

An independent read-only review of the saved design and the relevant current source found no
concrete contract or sequencing defects. It checked open/refresh/page cancellation, tenant-session
ownership, trigger identity, preview paging and the two scenarios' fixture path. Browser evidence
and implementation compilation remain the implementing owner's acceptance gates.
