import type { z } from "zod";
import type { Artist } from "@concertable/shared/features/artists/types";
import type {
  createArtistRequestSchema,
  updateArtistRequestSchema,
} from "./schemas/artistRequestSchemas";

export type { Artist } from "@concertable/shared/features/artists/types";

export type CreateArtistRequest = z.infer<typeof createArtistRequestSchema>;
export type UpdateArtistRequest = z.infer<typeof updateArtistRequestSchema>;

export function toUpdateArtistRequest(artist: Artist): UpdateArtistRequest {
  return {
    name: artist.name,
    about: artist.about,
    latitude: artist.latitude,
    longitude: artist.longitude,
    genres: artist.genres,
  };
}
