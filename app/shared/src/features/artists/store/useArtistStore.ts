import { create } from "zustand";
import type { Artist } from "@concertable/shared/features/artists";
import type { Genre, ImageFile } from "@concertable/shared/types";

interface ArtistStore {
  draft: Artist | undefined;
  banner: ImageFile | undefined;
  avatar: ImageFile | undefined;
  editMode: boolean;
  beginEdit: (artist: Artist) => void;
  endEdit: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setGenres: (genres: Genre[]) => void;
  setLocation: (
    latitude: number,
    longitude: number,
    county: string,
    town: string,
  ) => void;
  setBanner: (file: ImageFile) => void;
  setAvatar: (file: ImageFile) => void;
}

const notEditing = {
  draft: undefined,
  banner: undefined,
  avatar: undefined,
  editMode: false,
};

function updateDraft(
  state: ArtistStore,
  update: (draft: Artist) => Artist,
): Partial<ArtistStore> {
  return state.draft === undefined ? {} : { draft: update(state.draft) };
}

export const useArtistStore = create<ArtistStore>()((set) => ({
  ...notEditing,
  beginEdit: (artist) =>
    set({
      ...notEditing,
      draft: { ...artist, genres: [...artist.genres] },
      editMode: true,
    }),
  endEdit: () => set(notEditing),
  setName: (name) =>
    set((state) => updateDraft(state, (draft) => ({ ...draft, name }))),
  setAbout: (about) =>
    set((state) => updateDraft(state, (draft) => ({ ...draft, about }))),
  setGenres: (genres) =>
    set((state) =>
      updateDraft(state, (draft) => ({ ...draft, genres: [...genres] })),
    ),
  setLocation: (latitude, longitude, county, town) =>
    set((state) =>
      updateDraft(state, (draft) => ({
        ...draft,
        latitude,
        longitude,
        county,
        town,
      })),
    ),
  setBanner: (banner) =>
    set((state) => ({
      ...updateDraft(state, (draft) => ({ ...draft, bannerUrl: banner.uri })),
      banner: state.draft === undefined ? state.banner : banner,
    })),
  setAvatar: (avatar) =>
    set((state) => ({
      ...updateDraft(state, (draft) => ({ ...draft, avatar: avatar.uri })),
      avatar: state.draft === undefined ? state.avatar : avatar,
    })),
}));
