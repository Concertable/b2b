export { default as artistApi } from "./api/artistApi";
export { artistKeys, useArtistQuery } from "./hooks/useArtistQuery";
export {
  useCreateArtistMutation,
  useUpdateArtistMutation,
} from "./hooks/useArtistMutations";
export type {
  Artist,
  CreateArtistRequest,
  UpdateArtistRequest,
} from "./types";
