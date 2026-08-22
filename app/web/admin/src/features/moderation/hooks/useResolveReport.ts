import { toast } from "sonner";
import { useResolveReportMutation } from "./useResolveReportMutation";
import type { ResolveReportRequest } from "../schemas/resolveReportRequestSchema";

export function useResolveReport(reportId: number) {
  const { mutate, isPending } = useResolveReportMutation();

  const submit = (request: ResolveReportRequest, onDone: () => void) => {
    mutate(
      { id: reportId, request },
      {
        onSuccess: () => {
          toast.success("Report resolved");
          onDone();
        },
      },
    );
  };

  return { submit, isPending };
}
