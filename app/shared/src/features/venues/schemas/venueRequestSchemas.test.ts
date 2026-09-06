import { describe, expect, it } from "vitest";
import {
  createVenueRequestSchema,
  updateVenueRequestSchema,
} from "./venueRequestSchemas";

const banner = { uri: "banner", name: "banner.jpg", type: "image/jpeg" };
const avatar = { uri: "avatar", name: "avatar.jpg", type: "image/jpeg" };

describe("venue request schemas", () => {
  it("normalizes a complete create request", () => {
    expect(
      createVenueRequestSchema.parse({
        name: "  Example Venue  ",
        about: "  About  ",
        latitude: 51.5,
        longitude: -0.1,
        banner,
        avatar,
      }),
    ).toEqual({
      name: "Example Venue",
      about: "About",
      latitude: 51.5,
      longitude: -0.1,
      banner,
      avatar,
    });
  });

  it("requires create images and valid coordinates", () => {
    expect(
      createVenueRequestSchema.safeParse({
        name: "Example Venue",
        about: "About",
        latitude: 91,
        longitude: -181,
      }).success,
    ).toBe(false);
  });

  it("allows an update without replacement images", () => {
    expect(
      updateVenueRequestSchema.safeParse({
        name: "Example Venue",
        about: "About",
        latitude: 51.5,
        longitude: -0.1,
      }).success,
    ).toBe(true);
  });
});
