import { z } from "zod";
import type { Venue } from "@concertable/shared/features/venues/types";
import type { ImageFile } from "@concertable/shared/types/image";

function isImageFile(value: unknown): value is ImageFile {
  if (typeof value !== "object" || value === null) return false;
  const image = value as Partial<ImageFile>;
  return (
    typeof image.uri === "string" &&
    typeof image.name === "string" &&
    typeof image.type === "string"
  );
}

const imageFileSchema = z.custom<ImageFile>(isImageFile, {
  error: "Image is required",
});

const venueRequestFields = {
  name: z
    .string()
    .trim()
    .min(1, "Name is required")
    .max(100, "Name must be 100 characters or fewer"),
  about: z
    .string()
    .trim()
    .min(1, "About is required")
    .max(1000, "About must be 1000 characters or fewer"),
  latitude: z.number().min(-90).max(90),
  longitude: z.number().min(-180).max(180),
};

export const createVenueRequestSchema = z.object({
  ...venueRequestFields,
  banner: imageFileSchema,
  avatar: imageFileSchema,
});

export const updateVenueRequestSchema = z.object({
  ...venueRequestFields,
  banner: imageFileSchema.optional(),
  avatar: imageFileSchema.optional(),
});

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
