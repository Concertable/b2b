import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { useMountEffect } from "@concertable/shared/hooks/useMountEffect";
import type { ImageFile } from "@concertable/shared/types/image";
import type { Venue } from "@concertable/shared/features/venues/types";
import venueApi from "../api/venueApi";
import {
  toUpdateVenueRequest,
  updateVenueRequestSchema,
  type UpdateVenueRequest,
} from "../schemas/venueRequestSchemas";
import { useVenueStore } from "../store/useVenueStore";
import { venueKeys, useVenueQuery } from "./useVenueQuery";

export interface UseMyVenueOptions {
  onSuccess?: (saved: Venue) => void;
  afterSave?: () => Promise<void>;
  onToggleEdit?: () => void;
  onResetDraft?: () => void;
  extraDirty?: boolean;
}

export interface UseMyVenueResult {
  venue: Venue | undefined;
  isLoading: boolean;
  isError: boolean;
  draft: Venue | undefined;
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
  setLocation: (
    latitude: number,
    longitude: number,
    county: string,
    town: string,
  ) => void;
}

const emptyRequest: UpdateVenueRequest = {
  name: "",
  about: "",
  latitude: 0,
  longitude: 0,
};

export function useMyVenue(options?: UseMyVenueOptions): UseMyVenueResult {
  const query = useVenueQuery();
  const queryClient = useQueryClient();
  const venueDraft = useVenueStore((state) => state.draft);
  const editMode = useVenueStore((state) => state.editMode);
  const beginEdit = useVenueStore((state) => state.beginEdit);
  const endEdit = useVenueStore((state) => state.endEdit);
  const setStoreName = useVenueStore((state) => state.setName);
  const setStoreAbout = useVenueStore((state) => state.setAbout);
  const setStoreBanner = useVenueStore((state) => state.setBanner);
  const setStoreAvatar = useVenueStore((state) => state.setAvatar);
  const setStoreLocation = useVenueStore((state) => state.setLocation);
  const {
    handleSubmit,
    reset,
    setValue,
    trigger,
    formState: { errors, isDirty: venueIsDirty, isValid },
  } = useForm<UpdateVenueRequest>({
    resolver: zodResolver(updateVenueRequestSchema),
    defaultValues: emptyRequest,
    mode: "onChange",
  });

  useMountEffect(() => () => endEdit());

  const mutation = useMutation({
    mutationFn: async (request: UpdateVenueRequest) => {
      const saved = await venueApi.updateVenue(request);
      if (options?.afterSave) await options.afterSave();
      return saved;
    },
    onSuccess: (saved) => {
      queryClient.setQueryData(venueKeys.my(), saved);
      queryClient.setQueryData(venueKeys.byId(saved.id), saved);
      reset(toUpdateVenueRequest(saved));
      endEdit();
      options?.onSuccess?.(saved);
    },
  });

  const venue = query.data ?? undefined;
  const draft =
    editMode && venue && venueDraft ? { ...venue, ...venueDraft } : undefined;

  const resetDraft = () => {
    if (venue) reset(toUpdateVenueRequest(venue));
    endEdit();
    options?.onResetDraft?.();
  };

  const toggleEdit = () => {
    if (editMode) resetDraft();
    else if (venue) {
      reset(toUpdateVenueRequest(venue));
      void trigger();
      beginEdit(venue);
    }
    options?.onToggleEdit?.();
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
  const setLocation = (
    latitude: number,
    longitude: number,
    county: string,
    town: string,
  ) => {
    setStoreLocation(latitude, longitude, county, town);
    setValue("latitude", latitude, {
      shouldDirty: true,
      shouldValidate: true,
    });
    setValue("longitude", longitude, {
      shouldDirty: true,
      shouldValidate: true,
    });
  };

  const saveError = venueIsDirty
    ? errors.name?.message ??
      errors.about?.message ??
      errors.latitude?.message ??
      errors.longitude?.message ??
      errors.banner?.message ??
      errors.avatar?.message
    : undefined;

  return {
    venue,
    draft,
    isLoading: query.isLoading,
    isError: query.isError,
    editMode,
    isDirty: venueIsDirty || (options?.extraDirty ?? false),
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
    setLocation,
  };
}
