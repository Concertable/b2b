import { toast } from "sonner";
import { usePagination } from "@concertable/web/hooks/usePagination";
import { useReportsQuery } from "./useReportsQuery";
import { useHideMessageMutation } from "./useHideMessageMutation";
import { useRestoreMessageMutation } from "./useRestoreMessageMutation";

export function useReportsQueue() {
  const { params, nextPage, prevPage } = usePagination(10);
  const { data, isLoading, isError } = useReportsQuery(params);
  const { mutate: hide } = useHideMessageMutation();
  const { mutate: restore } = useRestoreMessageMutation();

  return {
    reports: data?.data,
    pageNumber: params.pageNumber,
    totalPages: data?.totalPages ?? 0,
    isLoading,
    isError,
    nextPage,
    prevPage,
    hideMessage: (id: number) =>
      hide(id, { onSuccess: () => toast.success("Message hidden") }),
    restoreMessage: (id: number) =>
      restore(id, { onSuccess: () => toast.success("Message restored") }),
  };
}
