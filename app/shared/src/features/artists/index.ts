export { default as artistApi } from "./api/artistApi";
export { artistKeys, useArtistQuery } from "./hooks/useArtistQuery";
export {
  useCreateArtistMutation,
  useUpdateArtistMutation,
} from "./hooks/useArtistMutations";
export { useArtist } from "./hooks/useArtist";
export type { UseArtistOptions, UseArtistResult } from "./hooks/useArtist";
export type {
  Artist,
  ArtistEditor,
  CreateArtistRequest,
  UpdateArtistRequest,
} from "./types";
