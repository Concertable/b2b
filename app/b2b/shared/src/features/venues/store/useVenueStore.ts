import { create } from "zustand";
import type { Venue } from "@concertable/shared/features/venues";
import type { ImageFile } from "@concertable/shared/types";

interface VenueStore {
  draft: Venue | undefined;
  banner: ImageFile | undefined;
  avatar: ImageFile | undefined;
  editMode: boolean;
  beginEdit: (venue: Venue) => void;
  endEdit: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
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
  state: VenueStore,
  update: (draft: Venue) => Venue,
): Partial<VenueStore> {
  return state.draft === undefined ? {} : { draft: update(state.draft) };
}

export const useVenueStore = create<VenueStore>()((set) => ({
  ...notEditing,
  beginEdit: (venue) => set({ ...notEditing, draft: { ...venue }, editMode: true }),
  endEdit: () => set(notEditing),
  setName: (name) =>
    set((state) => updateDraft(state, (draft) => ({ ...draft, name }))),
  setAbout: (about) =>
    set((state) => updateDraft(state, (draft) => ({ ...draft, about }))),
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
