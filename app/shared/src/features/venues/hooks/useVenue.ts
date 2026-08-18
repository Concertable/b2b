import type { UpdateVenueRequest, VenueEditor } from "../types";
import { useVenueStore } from "../store/useVenueStore";
import { useVenueQuery } from "./useVenueQuery";
import { useUpdateVenueMutation } from "./useVenueMutations";

export interface UseVenueOptions {
  onSuccess?: () => void;
  afterSave?: () => Promise<void>;
  onToggleEdit?: () => void;
  onResetDraft?: () => void;
  extraDirty?: boolean;
}

export interface UseVenueResult extends VenueEditor {
  venue: ReturnType<typeof useVenueQuery>["data"];
  isLoading: boolean;
  isError: boolean;
  isSaving: boolean;
  save: () => void;
  toggleEdit: () => void;
}

export function useVenue(options?: UseVenueOptions): UseVenueResult {
  const query = useVenueQuery();
  const draft = useVenueStore((state) => state.draft);
  const banner = useVenueStore((state) => state.banner);
  const avatar = useVenueStore((state) => state.avatar);
  const editMode = useVenueStore((state) => state.editMode);
  const beginEdit = useVenueStore((state) => state.beginEdit);
  const endEdit = useVenueStore((state) => state.endEdit);
  const setName = useVenueStore((state) => state.setName);
  const setAbout = useVenueStore((state) => state.setAbout);
  const setLocation = useVenueStore((state) => state.setLocation);
  const setBanner = useVenueStore((state) => state.setBanner);
  const setAvatar = useVenueStore((state) => state.setAvatar);
  const mutation = useUpdateVenueMutation();
  const isDirty =
    draft !== undefined &&
    (banner !== undefined ||
      avatar !== undefined ||
      JSON.stringify(draft) !== JSON.stringify(query.data) ||
      (options?.extraDirty ?? false));

  const resetDraft = () => {
    endEdit();
    options?.onResetDraft?.();
  };

  const save = () => {
    if (draft === undefined) return;
    const request: UpdateVenueRequest = {
      name: draft.name,
      about: draft.about,
      latitude: draft.latitude,
      longitude: draft.longitude,
      banner,
      avatar,
    };
    mutation.mutate(request, {
      onSuccess: async () => {
        await options?.afterSave?.();
        endEdit();
        options?.onSuccess?.();
      },
    });
  };

  return {
    venue: query.data,
    draft,
    isLoading: query.isLoading,
    isError: query.isError,
    isSaving: mutation.isPending,
    editMode,
    isDirty,
    save,
    toggleEdit: () => {
      if (editMode) endEdit();
      else if (query.data !== undefined) beginEdit(query.data);
      options?.onToggleEdit?.();
    },
    resetDraft,
    setName,
    setAbout,
    setLocation,
    setBanner,
    setAvatar,
  };
}
