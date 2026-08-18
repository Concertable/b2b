import type { Venue } from "@concertable/shared/features/venues";
import type { ImageFile } from "@concertable/shared/types";

export type { Venue } from "@concertable/shared/features/venues";

export interface CreateVenueRequest {
  name: string;
  about: string;
  latitude: number;
  longitude: number;
  banner: ImageFile;
  avatar: ImageFile;
}

export interface UpdateVenueRequest {
  name: string;
  about: string;
  latitude: number;
  longitude: number;
  banner?: ImageFile;
  avatar?: ImageFile;
}

export interface VenueEditor {
  draft: Venue | undefined;
  editMode: boolean;
  isDirty: boolean;
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
  resetDraft: () => void;
}
