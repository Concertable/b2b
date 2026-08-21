import { toast } from "sonner";
import { resolveReportRequestSchema } from "../schemas/resolveReportRequestSchema";
import { useResolveReportMutation } from "./useResolveReportMutation";
import type { ReportOutcome } from "../types";

export interface ResolveBuffer {
  outcome?: ReportOutcome;
  notes: string;
}

export function useResolveReport(reportId: number) {
  const { mutate, isPending } = useResolveReportMutation();

  const parse = (buffer: ResolveBuffer) =>
    resolveReportRequestSchema.safeParse({
      outcome: buffer.outcome,
      notes: buffer.notes.trim() || undefined,
    });

  const submit = (buffer: ResolveBuffer, onDone: () => void) => {
    const parsed = parse(buffer);
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
