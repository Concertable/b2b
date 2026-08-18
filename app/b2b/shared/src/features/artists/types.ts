import type { Artist } from "@concertable/shared/features/artists";
import type { Genre, ImageFile } from "@concertable/shared/types";

export type { Artist } from "@concertable/shared/features/artists";

export interface CreateArtistRequest {
  name: string;
  about: string;
  latitude: number;
  longitude: number;
  genres: Genre[];
  banner: ImageFile;
  avatar: ImageFile;
}

export interface UpdateArtistRequest {
  name: string;
  about: string;
  latitude: number;
  longitude: number;
  genres: Genre[];
  banner?: ImageFile;
  avatar?: ImageFile;
}

export interface ArtistEditor {
  draft: Artist | undefined;
  editMode: boolean;
  isDirty: boolean;
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
  resetDraft: () => void;
}
