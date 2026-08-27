import { beforeEach, describe, expect, it, vi } from "vitest";
import { usePendingVerifications } from "./usePendingVerifications";

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
vi.mock("./usePendingVerificationsQuery", () => ({
  usePendingVerificationsQuery: () => ({
    data: {
      data: [{ tenantId: "t-1" }],
      totalPages: 2,
    },
    isLoading: false,
    isError: false,
  }),
}));
vi.mock("./useApproveVerificationMutation", () => ({
  useApproveVerificationMutation: () => ({ mutate: mocks.approve }),
}));

describe("usePendingVerifications approval", () => {
  beforeEach(() => vi.clearAllMocks());

  it("returns to the previous page after approving its sole row", () => {
    const { approve } = usePendingVerifications();
    approve("t-1");

    const options = mocks.approve.mock.calls[0][1];
    options.onSuccess();

    expect(mocks.setPage).toHaveBeenCalledWith(1);
    expect(mocks.toastSuccess).toHaveBeenCalledWith("Organisation verified");
  });
});
