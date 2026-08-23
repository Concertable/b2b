import { beforeEach, describe, expect, it, vi } from "vitest";
import { usePendingVenues } from "./usePendingVenues";

const mocks = vi.hoisted(() => ({
  approve: vi.fn(),
  setPage: vi.fn(),
  toastSuccess: vi.fn(),
}));

vi.mock("sonner", () => ({
  toast: { success: mocks.toastSuccess },
}));
vi.mock("@concertable/web/hooks/usePagination", () => ({
  usePagination: () => ({
    params: { pageNumber: 2, pageSize: 10 },
    setPage: mocks.setPage,
    nextPage: vi.fn(),
    prevPage: vi.fn(),
  }),
}));
vi.mock("./usePendingVenuesQuery", () => ({
  usePendingVenuesQuery: () => ({
    data: {
      data: [{ id: 42 }],
      totalPages: 2,
    },
    isLoading: false,
    isError: false,
  }),
}));
vi.mock("./useApproveVenueMutation", () => ({
  useApproveVenueMutation: () => ({ mutate: mocks.approve }),
}));

describe("usePendingVenues approval", () => {
  beforeEach(() => vi.clearAllMocks());

  it("returns to the previous page after approving its sole venue", () => {
    const { approve } = usePendingVenues();
    approve(42);

    const options = mocks.approve.mock.calls[0][1];
    options.onSuccess();

    expect(mocks.setPage).toHaveBeenCalledWith(1);
    expect(mocks.toastSuccess).toHaveBeenCalledWith("Venue approved");
  });
});
