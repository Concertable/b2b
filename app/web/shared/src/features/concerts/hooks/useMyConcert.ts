import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import {
  updateConcertRequestSchema,
  type UpdateConcertRequest,
} from "@concertable/shared/features/concerts/schemas/updateConcertRequestSchema";
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
  canSave: boolean;
  saveError: string | null;
  save: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  toggleEdit: () => void;
  resetDraft: () => void;
}

function toRequest(concert: Concert): UpdateConcertRequest {
  return {
    name: concert.name,
    about: concert.about,
    price: concert.price,
    totalTickets: concert.totalTickets,
  };
}

export function useMyConcert(id: number): UseMyConcertResult {
  const { data: concert, isLoading, isError } = useMyConcertQuery(id);
  const queryClient = useQueryClient();

  const [editMode, setEditMode] = useState(false);
  const [buffer, setBuffer] = useState<UpdateConcertRequest | null>(null);

  const mutation = useMutation({
    mutationFn: (request: UpdateConcertRequest) =>
      concertApi.updateConcert(id, request),
    onSuccess: (saved) => {
      queryClient.setQueryData(myConcertQueryKey(id), saved);
      setBuffer(null);
      setEditMode(false);
    },
  });

  const patch = (fields: Partial<UpdateConcertRequest>) =>
    setBuffer((current) => (current ? { ...current, ...fields } : current));

  const validation = buffer
    ? updateConcertRequestSchema.safeParse(buffer)
    : undefined;
  const isDirty =
    !!buffer &&
    !!concert &&
    (buffer.name !== concert.name ||
      buffer.about !== concert.about ||
      buffer.price !== concert.price ||
      buffer.totalTickets !== concert.totalTickets);
  const canSave = validation?.success ?? false;
  const saveError =
    isDirty && validation && !validation.success
      ? validation.error.issues[0].message
      : null;

  return {
    concert,
    draft: editMode && buffer && concert ? { ...concert, ...buffer } : undefined,
    isLoading,
    isError,
    editMode,
    isDirty,
    canSave,
    saveError,
    isSaving: mutation.isPending,
    save: () => {
      if (validation?.success) mutation.mutate(validation.data);
    },
    setName: (name) => patch({ name }),
    setAbout: (about) => patch({ about }),
    toggleEdit: () => {
      if (!concert) return;
      if (editMode) {
        setBuffer(null);
        setEditMode(false);
      } else {
        setBuffer(toRequest(concert));
        setEditMode(true);
      }
    },
    resetDraft: () => {
      setBuffer(null);
      setEditMode(false);
    },
  };
}
