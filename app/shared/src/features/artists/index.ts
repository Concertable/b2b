export { default as artistApi } from "./api/artistApi";
export { artistKeys, useArtistQuery } from "./hooks/useArtistQuery";
export { useMyArtist } from "./hooks/useMyArtist";
export type {
  UseMyArtistOptions,
  UseMyArtistResult,
} from "./hooks/useMyArtist";
export { useCreateArtist } from "./hooks/useCreateArtist";
export type {
  UseCreateArtistOptions,
  UseCreateArtistResult,
} from "./hooks/useCreateArtist";
export {
  useCreateArtistMutation,
  useUpdateArtistMutation,
} from "./hooks/useArtistMutations";
export type {
  Artist,
  CreateArtistRequest,
  UpdateArtistRequest,
} from "@concertable/shared/features/artists/types";
