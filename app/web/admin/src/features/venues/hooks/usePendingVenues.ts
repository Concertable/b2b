import { toast } from "sonner";
import { usePagination } from "@concertable/web/hooks/usePagination";
import { usePendingVenuesQuery } from "./usePendingVenuesQuery";
import { useApproveVenueMutation } from "./useApproveVenueMutation";

export function usePendingVenues() {
  const { params, setPage, nextPage, prevPage } = usePagination(10);
  const { data, isLoading, isError } = usePendingVenuesQuery(params);
  const { mutate: approveMutation } = useApproveVenueMutation();

  return {
    venues: data?.data,
    pageNumber: params.pageNumber,
    totalPages: data?.totalPages ?? 0,
    isLoading,
    isError,
    nextPage,
    prevPage,
    approve: (id: number) =>
      approveMutation(id, {
        onSuccess: () => {
          if (params.pageNumber > 1 && data?.data.length === 1)
            setPage(params.pageNumber - 1);

          toast.success("Venue approved");
        },
      }),
  };
}
