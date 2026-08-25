import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import concertApi from "@concertable/shared/features/concerts/api/concertApi";
import { concertKeys } from "@concertable/shared/features/concerts/hooks/useConcertQuery";
import { updateConcertRequestSchema } from "@concertable/shared/features/concerts/schemas/updateConcertRequestSchema";
import type {
  Concert,
  UpdateConcertRequest,
} from "@concertable/shared/features/concerts/types";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import { useConcertStore } from "../store/useConcertStore";
import type { MyConcert } from "../types";
import { useMyConcertQuery } from "./useMyConcertQuery";

interface UseMyConcertResult {
  concert: MyConcert | undefined;
  draft:
    | Pick<Concert, "name" | "about" | "price" | "totalTickets">
    | undefined;
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
  const draft = useConcertStore((state) => state.draft);
  const editMode = useConcertStore((state) => state.editMode);
  const beginEdit = useConcertStore((state) => state.beginEdit);
  const endEdit = useConcertStore((state) => state.endEdit);
  const setStoreName = useConcertStore((state) => state.setName);
  const setStoreAbout = useConcertStore((state) => state.setAbout);
  const {
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isDirty, isValid },
  } = useForm<UpdateConcertRequest>({
    resolver: zodResolver(updateConcertRequestSchema),
    defaultValues: emptyRequest,
    mode: "onChange",
  });

  useMountEffect(() => () => endEdit());

  const mutation = useMutation({
    mutationFn: (request: UpdateConcertRequest) =>
      concertApi.updateConcert(id, request),
    onSuccess: (saved) => {
      queryClient.setQueryData<MyConcert>(concertKeys.my(id), (previous) =>
        previous ? { ...previous, ...saved } : undefined,
      );
      reset();
      endEdit();
    },
  });

  const resetDraft = () => {
    reset();
    endEdit();
  };

  const toggleEdit = () => {
    if (editMode) {
      resetDraft();
    } else if (concert) {
      reset(beginEdit(concert));
    }
  };

  const save = () => {
    void handleSubmit((request) => mutation.mutate(request))();
  };

  const setName = (name: string) => {
    setStoreName(name);
    setValue("name", name, { shouldDirty: true, shouldValidate: true });
  };

  const setAbout = (about: string) => {
    setStoreAbout(about);
    setValue("about", about, {
      shouldDirty: true,
      shouldValidate: true,
    });
  };

  const saveError = isDirty
    ? errors.name?.message ??
      errors.about?.message ??
      errors.price?.message ??
      errors.totalTickets?.message
    : undefined;

  return {
    concert,
    draft: editMode ? draft : undefined,
    isLoading,
    isError,
    editMode,
    isDirty,
    canSave: editMode && isDirty && isValid,
    saveError,
    save,
    resetDraft,
    toggleEdit,
    setName,
    setAbout,
    isSaving: mutation.isPending,
  };
}
