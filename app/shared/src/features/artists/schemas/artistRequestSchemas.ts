import { z } from "zod";
import { GENRE_VALUES } from "@concertable/shared/types";
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

const artistRequestFields = {
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
  genres: z.array(z.enum(GENRE_VALUES)),
};

export const createArtistRequestSchema = z.object({
  ...artistRequestFields,
  banner: imageFileSchema,
  avatar: imageFileSchema,
});

export const updateArtistRequestSchema = z.object({
  ...artistRequestFields,
  banner: imageFileSchema.optional(),
  avatar: imageFileSchema.optional(),
});
