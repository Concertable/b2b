import { z } from "zod";
import type { ImageFile } from "@concertable/shared/types/image";
import type {
  CreateVenueRequest,
  UpdateVenueRequest,
} from "@concertable/shared/features/venues/types";

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
}) satisfies z.ZodType<CreateVenueRequest>;

export const updateVenueRequestSchema = z.object({
  ...venueRequestFields,
  banner: imageFileSchema.optional(),
  avatar: imageFileSchema.optional(),
}) satisfies z.ZodType<UpdateVenueRequest>;
