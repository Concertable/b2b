import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import type { ImageFile } from "@concertable/shared/types/image";
import artistApi from "../api/artistApi";
import { updateArtistRequestSchema } from "../schemas/artistRequestSchemas";
import {
  toUpdateArtistRequest,
  type Artist,
  type UpdateArtistRequest,
} from "../types";
import { useArtistStore } from "../store/useArtistStore";
import { artistKeys, useArtistQuery } from "./useArtistQuery";

export interface UseMyArtistOptions {
  onSuccess?: (saved: Artist) => void;
}

export interface UseMyArtistResult {
  artist: Artist | undefined;
  draft: Artist | undefined;
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
  setBanner: (banner: ImageFile) => void;
  setAvatar: (avatar: ImageFile) => void;
}

const emptyRequest: UpdateArtistRequest = {
  name: "",
  about: "",
  latitude: 0,
  longitude: 0,
  genres: [],
};

export function useMyArtist(options?: UseMyArtistOptions): UseMyArtistResult {
  const query = useArtistQuery();
  const queryClient = useQueryClient();
  const artistDraft = useArtistStore((state) => state.draft);
  const editMode = useArtistStore((state) => state.editMode);
  const beginEdit = useArtistStore((state) => state.beginEdit);
  const endEdit = useArtistStore((state) => state.endEdit);
  const setStoreName = useArtistStore((state) => state.setName);
  const setStoreAbout = useArtistStore((state) => state.setAbout);
  const setStoreBanner = useArtistStore((state) => state.setBanner);
  const setStoreAvatar = useArtistStore((state) => state.setAvatar);
  const {
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isDirty, isValid },
  } = useForm<UpdateArtistRequest>({
    resolver: zodResolver(updateArtistRequestSchema),
    defaultValues: emptyRequest,
    mode: "onChange",
  });

  useMountEffect(() => () => endEdit());

  const mutation = useMutation({
    mutationFn: artistApi.updateArtist,
    onSuccess: (saved) => {
      queryClient.setQueryData(artistKeys.my(), saved);
      queryClient.setQueryData(artistKeys.byId(saved.id), saved);
      reset(toUpdateArtistRequest(saved));
      endEdit();
      options?.onSuccess?.(saved);
    },
  });

  const artist = query.data ?? undefined;
  const draft =
    editMode && artist && artistDraft
      ? { ...artist, ...artistDraft }
      : undefined;

  const resetDraft = () => {
    if (artist) reset(toUpdateArtistRequest(artist));
    endEdit();
  };

  const toggleEdit = () => {
    if (editMode) resetDraft();
    else if (artist) {
      reset(toUpdateArtistRequest(artist));
      beginEdit(artist);
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
    setValue("about", about, { shouldDirty: true, shouldValidate: true });
  };
  const setBanner = (banner: ImageFile) => {
    setStoreBanner(banner);
    setValue("banner", banner, { shouldDirty: true, shouldValidate: true });
  };
  const setAvatar = (avatar: ImageFile) => {
    setStoreAvatar(avatar);
    setValue("avatar", avatar, { shouldDirty: true, shouldValidate: true });
  };

  const saveError = isDirty
    ? errors.name?.message ??
      errors.about?.message ??
      errors.latitude?.message ??
      errors.longitude?.message ??
      errors.genres?.message ??
      errors.banner?.message ??
      errors.avatar?.message
    : undefined;

  return {
    artist,
    draft,
    isLoading: query.isLoading,
    isError: query.isError,
    editMode,
    isDirty,
    isSaving: mutation.isPending,
    canSave: isValid,
    saveError,
    save,
    toggleEdit,
    resetDraft,
    setName,
    setAbout,
    setBanner,
    setAvatar,
  };
}
