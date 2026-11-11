import type { ConversationMessage, ConversationReadPosition } from "./types";

export function getReadPosition(
  conversationId: number,
  messages: readonly ConversationMessage[],
): ConversationReadPosition | undefined {
  const throughSequence = messages.reduce(
    (highest, message) =>
      message.conversationId === conversationId
        ? Math.max(highest, message.sequence)
        : highest,
    0,
  );
  return throughSequence === 0
    ? undefined
    : { conversationId, throughSequence };
}
