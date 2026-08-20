import { beforeEach, describe, expect, it, vi } from "vitest";
import artistApi from "./artistApi";

const mocks = vi.hoisted(() => ({
  post: vi.fn(),
  put: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: {
    getOptional: vi.fn(),
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

describe("artistApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("FormData", CapturingFormData);
  });

  it("creates an artist with the complete multipart request", async () => {
    const banner = { uri: "banner", name: "banner.jpg", type: "image/jpeg" };
    const avatar = { uri: "avatar", name: "avatar.jpg", type: "image/jpeg" };
    const artist = { id: 42 };
    mocks.post.mockResolvedValue({ data: artist });

    await expect(
      artistApi.createArtist({
        name: "Example Artist",
        about: "About",
        latitude: 51.5,
        longitude: -0.1,
        genres: ["Rock", "Jazz"],
        banner,
        avatar,
      }),
    ).resolves.toBe(artist);

    const formData = mocks.post.mock.calls[0][1] as CapturingFormData;
    expect(mocks.post).toHaveBeenCalledWith("/organization/artist", formData);
    expect(formData.fields).toEqual([
      ["Name", "Example Artist"],
      ["About", "About"],
      ["Latitude", "51.5"],
      ["Longitude", "-0.1"],
      ["Genres[0]", "Rock"],
      ["Genres[1]", "Jazz"],
      ["Banner", banner],
      ["Avatar", avatar],
    ]);
  });

  it("omits optional images from an artist update", async () => {
    mocks.put.mockResolvedValue({ data: { id: 42 } });

    await artistApi.updateArtist({
      name: "Example Artist",
      about: "About",
      latitude: 51.5,
      longitude: -0.1,
      genres: ["Rock"],
    });

    const formData = mocks.put.mock.calls[0][1] as CapturingFormData;
    expect(formData.fields.map(([name]) => name)).toEqual([
      "Name",
      "About",
      "Latitude",
      "Longitude",
      "Genres[0]",
    ]);
  });
});
