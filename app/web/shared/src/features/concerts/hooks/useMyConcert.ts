import { useMutation, useQueryClient } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import { useConcertStore } from "@concertable/shared/features/concerts/store/useConcertStore";
import {
  updateConcertRequestSchema,
  type UpdateConcertRequest,
} from "@concertable/shared/features/concerts/schemas/updateConcertRequestSchema";
import type { Concert } from "@concertable/shared/features/concerts/types";
import { concertKeys } from "@concertable/shared/features/concerts/hooks/useConcertQuery";
import { useMyConcertQuery } from "./useMyConcertQuery";

interface UseMyConcertResult {
  concert: Concert | undefined;
  draft: Concert | undefined;
  isLoading: boolean;
  isError: boolean;
  editMode: boolean;
  isDirty: boolean;
  isSaving: boolean;
  canSave: boolean;
  saveError: string | null;
  save: () => void;
  toggleEdit: () => void;
  resetDraft: () => void;
}

export function useMyConcert(id: number): UseMyConcertResult {
  const { data: concert, isLoading, isError } = useMyConcertQuery(id);
  const queryClient = useQueryClient();

  const { beginEdit, endEdit, draft, isDirty, editMode } = useConcertStore();

  const mutation = useMutation({
    mutationFn: (request: UpdateConcertRequest) =>
      concertApi.updateConcert(id, request),
    onSuccess: (saved) => {
      queryClient.setQueryData(concertKeys.my(id), saved);
      endEdit();
    },
  });

  const validation = draft
    ? updateConcertRequestSchema.safeParse(draft)
    : undefined;
  const canSave = validation?.success ?? false;
  const saveError =
    isDirty && validation && !validation.success
      ? validation.error.issues[0].message
      : null;

  const save = () => {
    if (validation?.success) mutation.mutate(validation.data);
  };

  return {
    concert,
    draft,
    isLoading,
    isError,
    editMode,
    isDirty,
    canSave,
    saveError,
    save,
    isSaving: mutation.isPending,
    toggleEdit: () => (editMode ? endEdit() : beginEdit(concert!)),
    resetDraft: endEdit,
  };
}
