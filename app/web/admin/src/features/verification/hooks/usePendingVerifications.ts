import { toast } from "sonner";
import { usePagination } from "@concertable/web/hooks/usePagination";
import { usePendingVerificationsQuery } from "./usePendingVerificationsQuery";
import { useApproveVerificationMutation } from "./useApproveVerificationMutation";

export function usePendingVerifications() {
  const { params, setPage, nextPage, prevPage } = usePagination(10);
  const { data, isLoading, isError } = usePendingVerificationsQuery(params);
  const { mutate: approveMutation } = useApproveVerificationMutation();

  return {
    verifications: data?.data,
    pageNumber: params.pageNumber,
    totalPages: data?.totalPages ?? 0,
    isLoading,
    isError,
    nextPage,
    prevPage,
    approve: (tenantId: string) =>
      approveMutation(tenantId, {
        onSuccess: () => {
          if (params.pageNumber > 1 && data?.data.length === 1)
            setPage(params.pageNumber - 1);

          toast.success("Organisation verified");
        },
      }),
  };
}
