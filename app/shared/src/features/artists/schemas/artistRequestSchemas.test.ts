import { describe, expect, it } from "vitest";
import {
  createArtistRequestSchema,
  updateArtistRequestSchema,
} from "./artistRequestSchemas";
import { toUpdateArtistRequest } from "../types";

const banner = { uri: "banner", name: "banner.jpg", type: "image/jpeg" };
const avatar = { uri: "avatar", name: "avatar.jpg", type: "image/jpeg" };

describe("artist request schemas", () => {
  it("normalizes a complete create request", () => {
    expect(
      createArtistRequestSchema.parse({
        name: "  Example Artist  ",
        about: "  About  ",
        latitude: 51.5,
        longitude: -0.1,
        genres: ["rock"],
        banner,
        avatar,
      }),
    ).toEqual({
      name: "Example Artist",
      about: "About",
      latitude: 51.5,
      longitude: -0.1,
      genres: ["rock"],
      banner,
      avatar,
    });
  });

  it("requires create images and valid coordinates", () => {
    expect(
      createArtistRequestSchema.safeParse({
        name: "Example Artist",
        about: "About",
        latitude: 91,
        longitude: -181,
        genres: [],
      }).success,
    ).toBe(false);
  });

  it("allows an update without replacement images", () => {
    expect(
      updateArtistRequestSchema.safeParse({
        name: "Example Artist",
        about: "About",
        latitude: 51.5,
        longitude: -0.1,
        genres: [],
      }).success,
    ).toBe(true);
  });

  it("projects only writable fields into an update request", () => {
    expect(
      toUpdateArtistRequest({
        id: 42,
        name: "Example Artist",
        about: "About",
        bannerUrl: "banner",
        avatar: "avatar",
        rating: 4.5,
        genres: ["rock", "jazz"],
        email: "artist@example.com",
        county: "Greater London",
        town: "London",
        latitude: 51.5,
        longitude: -0.1,
      }),
    ).toEqual({
      name: "Example Artist",
      about: "About",
      latitude: 51.5,
      longitude: -0.1,
      genres: ["rock", "jazz"],
    });
  });
});
