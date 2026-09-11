import { beforeEach, describe, expect, it, vi } from "vitest";
import venueApi from "./venueApi";

const mocks = vi.hoisted(() => ({
  getOptional: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: {
    getOptional: mocks.getOptional,
    post: mocks.post,
    put: mocks.put,
  },
}));

class CapturingFormData {
  readonly fields: Array<[string, unknown]> = [];

  append(name: string, value: unknown) {
    this.fields.push([name, value]);
  }
}

describe("venueApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("FormData", CapturingFormData);
  });

  it("preserves the organization-profile route's null result", async () => {
    mocks.getOptional.mockResolvedValue({ data: null });

    await expect(venueApi.getVenue()).resolves.toBeNull();

    expect(mocks.getOptional).toHaveBeenCalledWith("/organization/venue");
  });

  it("creates a venue with the complete multipart request", async () => {
    const banner = { uri: "banner", name: "banner.jpg", type: "image/jpeg" };
    const avatar = { uri: "avatar", name: "avatar.jpg", type: "image/jpeg" };
    const venue = { id: 42 };
    mocks.post.mockResolvedValue({ data: venue });

    await expect(
      venueApi.createVenue({
        name: "Example Venue",
        about: "About",
        latitude: 51.5,
        longitude: -0.1,
        banner,
        avatar,
      }),
    ).resolves.toBe(venue);

    const formData = mocks.post.mock.calls[0][1] as CapturingFormData;
    expect(mocks.post).toHaveBeenCalledWith("/organization/venue", formData);
    expect(formData.fields).toEqual([
      ["Name", "Example Venue"],
      ["About", "About"],
      ["Latitude", "51.5"],
      ["Longitude", "-0.1"],
      ["Banner", banner],
      ["Avatar", avatar],
    ]);
  });

  it("omits optional images from a venue update", async () => {
    mocks.put.mockResolvedValue({ data: { id: 42 } });

    await venueApi.updateVenue({
      name: "Example Venue",
      about: "About",
      latitude: 51.5,
      longitude: -0.1,
    });

    const formData = mocks.put.mock.calls[0][1] as CapturingFormData;
    expect(formData.fields.map(([name]) => name)).toEqual([
      "Name",
      "About",
      "Latitude",
      "Longitude",
    ]);
  });
});
