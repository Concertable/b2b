import { create } from "zustand";
import { immer } from "zustand/middleware/immer";
import type { Concert } from "@concertable/shared/features/concerts";

export interface ConcertState {
  draft:
    | Pick<Concert, "name" | "about" | "price" | "totalTickets">
    | undefined;
  editMode: boolean;
  beginEdit: (concert: NonNullable<ConcertState["draft"]>) => void;
  endEdit: () => void;
  setName: (name: string) => void;
  setAbout: (about: string) => void;
  setPrice: (price: number) => void;
  setTotalTickets: (totalTickets: number) => void;
}

export const useConcertStore = create<ConcertState>()(
  immer((set) => ({
    draft: undefined,
    editMode: false,
    beginEdit: (concert) =>
      set((state) => {
        state.draft = {
          name: concert.name,
          about: concert.about,
          price: concert.price,
          totalTickets: concert.totalTickets,
        };
        state.editMode = true;
      }),
    endEdit: () =>
      set((state) => {
        state.draft = undefined;
        state.editMode = false;
      }),
    setName: (name) =>
      set((state) => {
        if (state.draft) state.draft.name = name;
      }),
    setAbout: (about) =>
      set((state) => {
        if (state.draft) state.draft.about = about;
      }),
    setPrice: (price) =>
      set((state) => {
        if (state.draft) state.draft.price = price;
      }),
    setTotalTickets: (totalTickets) =>
      set((state) => {
        if (state.draft) state.draft.totalTickets = totalTickets;
      }),
  })),
);
