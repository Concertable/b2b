import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import { concertKeys } from "@concertable/shared/features/concerts/hooks/useConcertQuery";
import { updateConcertRequestSchema } from "@concertable/shared/features/concerts/schemas/updateConcertRequestSchema";
import {
  Concert,
  type UpdateConcertRequest,
} from "@concertable/shared/features/concerts/types";
import type { MyConcert } from "../types";
import { useMyConcertQuery } from "./useMyConcertQuery";

interface UseMyConcertResult {
  concert: MyConcert | undefined;
  draft: UpdateConcertRequest | undefined;
  isLoading: boolean;
  isError: boolean;
  editMode: boolean;
  isDirty: boolean;
  isSaving: boolean;
  canSave: boolean;
  saveError?: string;
  save: () => void;
  toggleEdit: () => void;
  resetDraft: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
}

const emptyRequest: UpdateConcertRequest = {
  name: "",
  about: "",
  price: 0,
  totalTickets: 0,
};

export function useMyConcert(id: number): UseMyConcertResult {
  const { data: concert, isLoading, isError } = useMyConcertQuery(id);
  const queryClient = useQueryClient();
  const [editMode, setEditMode] = useState(false);
  const {
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors, isDirty, isValid },
  } = useForm<UpdateConcertRequest>({
    resolver: zodResolver(updateConcertRequestSchema),
    defaultValues: emptyRequest,
    mode: "onChange",
  });

  const mutation = useMutation({
    mutationFn: (request: UpdateConcertRequest) =>
      concertApi.updateConcert(id, request),
    onSuccess: (saved) => {
      queryClient.setQueryData<MyConcert>(concertKeys.my(id), (previous) =>
        previous ? { ...previous, ...saved } : undefined,
      );
      reset(Concert.toUpdateRequest(saved));
      setEditMode(false);
    },
  });

  const resetDraft = () => {
    if (concert) reset(Concert.toUpdateRequest(concert));
    setEditMode(false);
  };

  const toggleEdit = () => {
    if (editMode) {
      resetDraft();
      return;
    }

    if (concert) {
      reset(Concert.toUpdateRequest(concert));
      setEditMode(true);
    }
  };

  const save = () => {
    void handleSubmit((request) => mutation.mutate(request))();
  };

  const saveError = isDirty
    ? errors.name?.message ??
      errors.about?.message ??
      errors.price?.message ??
      errors.totalTickets?.message
    : undefined;

  return {
    concert,
    draft: editMode ? watch() : undefined,
    isLoading,
    isError,
    editMode,
    isDirty,
    canSave: editMode && isDirty && isValid,
    saveError,
    save,
    resetDraft,
    toggleEdit,
    setName: (name) =>
      setValue("name", name, { shouldDirty: true, shouldValidate: true }),
    setAbout: (about) =>
      setValue("about", about, {
        shouldDirty: true,
        shouldValidate: true,
      }),
    isSaving: mutation.isPending,
  };
}
