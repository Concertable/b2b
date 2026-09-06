import type { z } from "zod";
import type { Venue } from "@concertable/shared/features/venues/types";
import type {
  createVenueRequestSchema,
  updateVenueRequestSchema,
} from "./schemas/venueRequestSchemas";

export type { Venue } from "@concertable/shared/features/venues/types";

export type CreateVenueRequest = z.infer<typeof createVenueRequestSchema>;
export type UpdateVenueRequest = z.infer<typeof updateVenueRequestSchema>;

export function toUpdateVenueRequest(venue: Venue): UpdateVenueRequest {
  return {
    name: venue.name,
    about: venue.about,
    latitude: venue.latitude,
    longitude: venue.longitude,
  };
}
