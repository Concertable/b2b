import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import type { ImageFile } from "@concertable/shared/types/image";
import artistApi from "../api/artistApi";
import { createArtistRequestSchema } from "../schemas/artistRequestSchemas";
import type { Artist, CreateArtistRequest } from "../types";
import { useArtistStore, type ArtistState } from "../store/useArtistStore";
import { artistKeys } from "./useArtistQuery";

export interface UseCreateArtistOptions {
  onSuccess?: (saved: Artist) => void;
}

export interface UseCreateArtistResult {
  draft: Artist;
  isCreating: boolean;
  canCreate: boolean;
  createError?: string;
  create: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setBanner: (banner: ImageFile) => void;
  setAvatar: (avatar: ImageFile) => void;
}

const initialArtist = {
  name: "",
  about: "",
  bannerUrl: "",
  avatar: undefined,
  genres: [],
  county: "",
  town: "",
  latitude: 51.5074,
  longitude: -0.1278,
} satisfies NonNullable<ArtistState["draft"]>;

const defaultValues = {
  name: initialArtist.name,
  about: initialArtist.about,
  latitude: initialArtist.latitude,
  longitude: initialArtist.longitude,
  genres: initialArtist.genres,
} satisfies Partial<CreateArtistRequest>;

export function useCreateArtist(
  options?: UseCreateArtistOptions,
): UseCreateArtistResult {
  const queryClient = useQueryClient();
  const artistDraft = useArtistStore((state) => state.draft);
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
    formState: { errors, isValid },
  } = useForm<CreateArtistRequest>({
    resolver: zodResolver(createArtistRequestSchema),
    defaultValues,
    mode: "onChange",
  });

  useMountEffect(() => {
    beginEdit(initialArtist);
    reset(defaultValues);
    return endEdit;
  });

  const mutation = useMutation({
    mutationFn: artistApi.createArtist,
    onSuccess: (saved) => {
      queryClient.setQueryData(artistKeys.my(), saved);
      queryClient.setQueryData(artistKeys.byId(saved.id), saved);
      endEdit();
      options?.onSuccess?.(saved);
    },
  });

  const state = artistDraft ?? initialArtist;
  const draft: Artist = {
    id: 0,
    rating: 0,
    email: "",
    ...state,
  };

  const create = () => {
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

  const createError =
    errors.name?.message ??
    errors.about?.message ??
    errors.latitude?.message ??
    errors.longitude?.message ??
    errors.genres?.message ??
    errors.banner?.message ??
    errors.avatar?.message;

  return {
    draft,
    isCreating: mutation.isPending,
    canCreate: isValid,
    createError,
    create,
    setName,
    setAbout,
    setBanner,
    setAvatar,
  };
}
