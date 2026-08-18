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
