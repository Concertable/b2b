import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import type { ImageFile } from "@concertable/shared/types/image";
import venueApi from "../api/venueApi";
import { createVenueRequestSchema } from "../schemas/venueRequestSchemas";
import type { CreateVenueRequest, Venue } from "../types";
import { useVenueStore, type VenueState } from "../store/useVenueStore";
import { venueKeys } from "./useVenueQuery";

export interface UseCreateVenueOptions {
  onSuccess?: (saved: Venue) => void;
}

export interface UseCreateVenueResult {
  draft: Venue;
  isCreating: boolean;
  canCreate: boolean;
  createError?: string;
  create: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setBanner: (banner: ImageFile) => void;
  setAvatar: (avatar: ImageFile) => void;
}

const initialVenue = {
  name: "",
  about: "",
  bannerUrl: "",
  avatar: undefined,
  county: "",
  town: "",
  latitude: 51.5074,
  longitude: -0.1278,
} satisfies NonNullable<VenueState["draft"]>;

const defaultValues = {
  name: initialVenue.name,
  about: initialVenue.about,
  latitude: initialVenue.latitude,
  longitude: initialVenue.longitude,
} satisfies Partial<CreateVenueRequest>;

export function useCreateVenue(
  options?: UseCreateVenueOptions,
): UseCreateVenueResult {
  const queryClient = useQueryClient();
  const venueDraft = useVenueStore((state) => state.draft);
  const beginEdit = useVenueStore((state) => state.beginEdit);
  const endEdit = useVenueStore((state) => state.endEdit);
  const setStoreName = useVenueStore((state) => state.setName);
  const setStoreAbout = useVenueStore((state) => state.setAbout);
  const setStoreBanner = useVenueStore((state) => state.setBanner);
  const setStoreAvatar = useVenueStore((state) => state.setAvatar);
  const {
    handleSubmit,
    reset,
    setValue,
    formState: { errors, isValid },
  } = useForm<CreateVenueRequest>({
    resolver: zodResolver(createVenueRequestSchema),
    defaultValues,
    mode: "onChange",
  });

  useMountEffect(() => {
    beginEdit(undefined, initialVenue);
    reset(defaultValues);
    return endEdit;
  });

  const mutation = useMutation({
    mutationFn: venueApi.createVenue,
    onSuccess: (saved) => {
      void queryClient.invalidateQueries({ queryKey: venueKeys.my() });
      queryClient.setQueryData(venueKeys.byId(saved.id), saved);
      endEdit();
      options?.onSuccess?.(saved);
    },
  });

  const state = venueDraft ?? initialVenue;
  const draft: Venue = {
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
