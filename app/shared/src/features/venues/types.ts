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
