import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Artist,
  CreateArtistRequest,
  UpdateArtistRequest,
} from "../types";

type FormDataValue = Parameters<FormData["append"]>[1];

function appendArtistFields(
  formData: FormData,
  request: CreateArtistRequest | UpdateArtistRequest,
): void {
  formData.append("Name", request.name);
  formData.append("About", request.about);
  formData.append("Latitude", String(request.latitude));
  formData.append("Longitude", String(request.longitude));
  request.genres.forEach((genre, index) => {
    formData.append(`Genres[${index}]`, genre);
  });
  if (request.banner) {
    formData.append("Banner", request.banner as unknown as FormDataValue);
  }
  if (request.avatar) {
    formData.append("Avatar", request.avatar as unknown as FormDataValue);
  }
}

const artistApi = {
  getArtist: async (): Promise<Artist | null> => {
    const { data } = await apiClient.getOptional<Artist>("/organization/artist");
    return data;
  },

  createArtist: async (request: CreateArtistRequest): Promise<Artist> => {
    const formData = new FormData();
    appendArtistFields(formData, request);
    const { data } = await apiClient.post<Artist>("/organization/artist", formData);
    return data;
  },

  updateArtist: async (request: UpdateArtistRequest): Promise<Artist> => {
    const formData = new FormData();
    appendArtistFields(formData, request);
    const { data } = await apiClient.put<Artist>("/organization/artist", formData);
    return data;
  },
};

export default artistApi;
