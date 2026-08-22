import { toast } from "sonner";
import { resolveReportRequestSchema } from "../schemas/resolveReportRequestSchema";
import { useResolveReportMutation } from "./useResolveReportMutation";
import type { ReportOutcome } from "../types";

export interface ResolveDraft {
  outcome?: ReportOutcome;
  notes: string;
}

export function useResolveReport(reportId: number) {
  const { mutate, isPending } = useResolveReportMutation();

  const parse = (draft: ResolveDraft) =>
    resolveReportRequestSchema.safeParse({
      outcome: draft.outcome,
      notes: draft.notes.trim() || undefined,
    });

  const submit = (draft: ResolveDraft, onDone: () => void) => {
    const parsed = parse(draft);
    if (parsed.success)
      mutate(
        { id: reportId, request: parsed.data },
        {
          onSuccess: () => {
            toast.success("Report resolved");
            onDone();
          },
        },
      );
    return parsed;
  };

  return { parse, submit, isPending };
}
