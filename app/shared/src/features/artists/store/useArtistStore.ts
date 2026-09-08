import { create } from "zustand";
import { immer } from "zustand/middleware/immer";
import type { ImageFile } from "@concertable/shared/types/image";
import type { Artist } from "@concertable/shared/features/artists/types";

export interface ArtistState {
  draft:
    | Pick<
        Artist,
        | "name"
        | "about"
        | "bannerUrl"
        | "avatar"
        | "genres"
        | "county"
        | "town"
        | "latitude"
        | "longitude"
      >
    | undefined;
  banner: ImageFile | undefined;
  avatar: ImageFile | undefined;
  editMode: boolean;
  beginEdit: (artist: NonNullable<ArtistState["draft"]>) => void;
  endEdit: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setBanner: (banner: ImageFile) => void;
  setAvatar: (avatar: ImageFile) => void;
}

export const useArtistStore = create<ArtistState>()(
  immer((set) => ({
    draft: undefined,
    banner: undefined,
    avatar: undefined,
    editMode: false,
    beginEdit: (artist) =>
      set((state) => {
        state.draft = {
          name: artist.name,
          about: artist.about,
          bannerUrl: artist.bannerUrl,
          avatar: artist.avatar,
          genres: [...artist.genres],
          county: artist.county,
          town: artist.town,
          latitude: artist.latitude,
          longitude: artist.longitude,
        };
        state.banner = undefined;
        state.avatar = undefined;
        state.editMode = true;
      }),
    endEdit: () =>
      set((state) => {
        state.draft = undefined;
        state.banner = undefined;
        state.avatar = undefined;
        state.editMode = false;
      }),
    setName: (name) =>
      set((state) => {
        if (state.draft) state.draft.name = name;
      }),
    setAbout: (about) =>
      set((state) => {
        if (state.draft) state.draft.about = about;
      }),
    setBanner: (banner) =>
      set((state) => {
        if (!state.draft) return;
        state.draft.bannerUrl = banner.uri;
        state.banner = banner;
      }),
    setAvatar: (avatar) =>
      set((state) => {
        if (!state.draft) return;
        state.draft.avatar = avatar.uri;
        state.avatar = avatar;
      }),
  })),
);
