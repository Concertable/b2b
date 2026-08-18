import type { ArtistEditor, UpdateArtistRequest } from "../types";
import { useArtistStore } from "../store/useArtistStore";
import { useArtistQuery } from "./useArtistQuery";
import { useUpdateArtistMutation } from "./useArtistMutations";

export interface UseArtistOptions {
  onSuccess?: () => void;
}

export interface UseArtistResult extends ArtistEditor {
  artist: ReturnType<typeof useArtistQuery>["data"];
  isLoading: boolean;
  isError: boolean;
  isSaving: boolean;
  save: () => void;
  toggleEdit: () => void;
}

export function useArtist(options?: UseArtistOptions): UseArtistResult {
  const query = useArtistQuery();
  const draft = useArtistStore((state) => state.draft);
  const banner = useArtistStore((state) => state.banner);
  const avatar = useArtistStore((state) => state.avatar);
  const editMode = useArtistStore((state) => state.editMode);
  const beginEdit = useArtistStore((state) => state.beginEdit);
  const resetDraft = useArtistStore((state) => state.endEdit);
  const setName = useArtistStore((state) => state.setName);
  const setAbout = useArtistStore((state) => state.setAbout);
  const setGenres = useArtistStore((state) => state.setGenres);
  const setLocation = useArtistStore((state) => state.setLocation);
  const setBanner = useArtistStore((state) => state.setBanner);
  const setAvatar = useArtistStore((state) => state.setAvatar);
  const mutation = useUpdateArtistMutation();
  const isDirty =
    draft !== undefined &&
    (banner !== undefined ||
      avatar !== undefined ||
      JSON.stringify(draft) !== JSON.stringify(query.data));

  const save = () => {
    if (draft === undefined) return;
    const request: UpdateArtistRequest = {
      name: draft.name,
      about: draft.about,
      latitude: draft.latitude,
      longitude: draft.longitude,
      genres: draft.genres,
      banner,
      avatar,
    };
    mutation.mutate(request, {
      onSuccess: () => {
        resetDraft();
        options?.onSuccess?.();
      },
    });
  };

  return {
    artist: query.data,
    draft,
    isLoading: query.isLoading,
    isError: query.isError,
    isSaving: mutation.isPending,
    editMode,
    isDirty,
    save,
    toggleEdit: () => {
      if (editMode) resetDraft();
      else if (query.data !== undefined) beginEdit(query.data);
    },
    resetDraft,
    setName,
    setAbout,
    setGenres,
    setLocation,
    setBanner,
    setAvatar,
  };
}
