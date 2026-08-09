import { beforeEach, describe, expect, it, vi } from "vitest";
import { useAcceptApplicationMutation } from "./useApplicationQuery";

const mocks = vi.hoisted(() => ({
  invalidateQueries: vi.fn(),
  setQueryData: vi.fn(),
  useMutation: vi.fn((options) => options),
}));

vi.mock("@tanstack/react-query", () => ({
  useMutation: mocks.useMutation,
  useQuery: vi.fn(),
  useQueryClient: () => ({
    invalidateQueries: mocks.invalidateQueries,
    setQueryData: mocks.setQueryData,
  }),
}));

vi.mock("../api/applicationApi", () => ({
  default: { acceptApplication: vi.fn() },
}));

describe("useAcceptApplicationMutation", () => {
  beforeEach(() => vi.clearAllMocks());

  it("marks the cached application as accepted", () => {
    useAcceptApplicationMutation(11);
    const options = mocks.useMutation.mock.calls[0][0];

    options.onSuccess(undefined, { applicationId: 42 });

    expect(mocks.setQueryData).toHaveBeenCalledWith(
      ["applications", 42],
      expect.any(Function),
    );
    const update = mocks.setQueryData.mock.calls[0][1];
    expect(update({ id: 42, status: "AwaitingPayment" })).toEqual({
      id: 42,
      status: "Accepted",
    });
    expect(mocks.invalidateQueries).toHaveBeenCalledWith({
      queryKey: ["applications", "opportunity", 11],
    });
  });
});
