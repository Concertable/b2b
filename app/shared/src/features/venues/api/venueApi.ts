import { apiClient } from "@concertable/shared/lib/apiClient";
import type {
  Venue,
  CreateVenueRequest,
  UpdateVenueRequest,
} from "@concertable/shared/features/venues/types";

type FormDataValue = Parameters<FormData["append"]>[1];

function appendVenueFields(
  formData: FormData,
  request: CreateVenueRequest | UpdateVenueRequest,
): void {
  formData.append("Name", request.name);
  formData.append("About", request.about);
  formData.append("Latitude", String(request.latitude));
  formData.append("Longitude", String(request.longitude));
  if (request.banner) {
    formData.append("Banner", request.banner as unknown as FormDataValue);
  }
  if (request.avatar) {
    formData.append("Avatar", request.avatar as unknown as FormDataValue);
  }
}

const venueApi = {
  getVenue: async (): Promise<Venue | null> => {
    const { data } = await apiClient.getOptional<Venue>("/organization/venue");
    return data;
  },

  createVenue: async (request: CreateVenueRequest): Promise<Venue> => {
    const formData = new FormData();
    appendVenueFields(formData, request);
    const { data } = await apiClient.post<Venue>("/organization/venue", formData);
    return data;
  },

  updateVenue: async (request: UpdateVenueRequest): Promise<Venue> => {
    const formData = new FormData();
    appendVenueFields(formData, request);
    const { data } = await apiClient.put<Venue>("/organization/venue", formData);
    return data;
  },
};

export default venueApi;
