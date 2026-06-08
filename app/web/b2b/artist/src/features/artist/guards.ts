import { redirect } from "@tanstack/react-router";
import { isAxiosError } from "axios";
import artistApi from "@concertable/shared/features/artists/api/artistApi";

export async function requireArtist({ pathname }: { pathname: string }) {
  if (pathname === "/create") return;
  try {
    const artist = await artistApi.getMyArtist();
    if (artist === null) throw redirect({ to: "/create" });
  } catch (e) {
    if (e instanceof Response || (e as any)?.isRedirect) throw e;
    if (isAxiosError(e) && e.response?.status === 401) throw redirect({ to: "/login" });
    throw e;
  }
}
