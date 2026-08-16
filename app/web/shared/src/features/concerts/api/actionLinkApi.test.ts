import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import actionLinkApi from "./actionLinkApi";

const mocks = vi.hoisted(() => ({
  request: vi.fn(),
}));

vi.mock("@concertable/shared/lib/apiClient", () => ({
  apiClient: { request: mocks.request },
}));

describe("actionLinkApi", () => {
  beforeEach(() => vi.clearAllMocks());
  afterEach(() => vi.unstubAllGlobals());

  it("executes the advertised method without duplicating the API prefix", async () => {
    mocks.request.mockResolvedValue({});

    await actionLinkApi.execute({
      href: "/api/application/42/withdraw",
      method: "POST",
    });

    expect(mocks.request).toHaveBeenCalledWith({
      url: "/Application/42/withdraw",
      method: "POST",
    });
  });

  it("downloads the advertised response and releases the object URL", async () => {
    const blob = new Blob(["contract"]);
    const anchor = {
      href: "",
      download: "",
      click: vi.fn(),
      remove: vi.fn(),
    };
    const appendChild = vi.fn();
    const createObjectURL = vi.fn(() => "blob:contract");
    const revokeObjectURL = vi.fn();
    mocks.request.mockResolvedValue({ data: blob });
    vi.stubGlobal("document", {
      createElement: vi.fn(() => anchor),
      body: { appendChild },
    });
    vi.stubGlobal("URL", { createObjectURL, revokeObjectURL });

    await actionLinkApi.download(
      { href: "/api/application/42/contract/pdf", method: "GET" },
      "contract-42.pdf",
    );

    expect(mocks.request).toHaveBeenCalledWith({
      url: "/Application/42/contract/pdf",
      method: "GET",
      responseType: "blob",
    });
    expect(createObjectURL).toHaveBeenCalledWith(blob);
    expect(anchor.href).toBe("blob:contract");
    expect(anchor.download).toBe("contract-42.pdf");
    expect(appendChild).toHaveBeenCalledWith(anchor);
    expect(anchor.click).toHaveBeenCalledOnce();
    expect(anchor.remove).toHaveBeenCalledOnce();
    expect(revokeObjectURL).toHaveBeenCalledWith("blob:contract");
  });
});
