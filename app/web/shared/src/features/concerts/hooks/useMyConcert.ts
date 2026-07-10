import { useMutation, useQueryClient } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import { useConcertStore } from "@concertable/shared/features/concerts/store/useConcertStore";
import type { Concert } from "@concertable/shared/features/concerts/types";
import { useMyConcertQuery, myConcertQueryKey } from "./useMyConcertQuery";

interface UseMyConcertResult {
  concert: Concert | undefined;
  draft: Concert | undefined;
  isLoading: boolean;
  isError: boolean;
  editMode: boolean;
  isDirty: boolean;
  isSaving: boolean;
  save: () => void;
  toggleEdit: () => void;
  resetDraft: () => void;
}

export function useMyConcert(id: number): UseMyConcertResult {
  const { data: concert, isLoading, isError } = useMyConcertQuery(id);
  const queryClient = useQueryClient();

  const { toggleEdit, resetDraft, draft, isDirty, editMode } =
    useConcertStore();

  const mutation = useMutation({
    mutationFn: () =>
      concertApi.updateConcert(id, {
        name: draft!.name,
        about: draft!.about,
        price: draft!.price,
        totalTickets: draft!.totalTickets,
      }),
    onSuccess: (saved) => {
      queryClient.setQueryData(myConcertQueryKey(id), saved);
      resetDraft(saved);
    },
  });

  return {
    concert,
    draft,
    isLoading,
    isError,
    editMode,
    isDirty,
    save: mutation.mutate,
    isSaving: mutation.isPending,
    toggleEdit: () => toggleEdit(concert!),
    resetDraft: () => resetDraft(concert!),
  };
}
