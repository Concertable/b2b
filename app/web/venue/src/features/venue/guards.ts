import { redirect } from "@tanstack/react-router";
import { isApiError } from "@concertable/shared/lib/apiError";
import { venueApi } from "@concertable/b2b/features/venues";

export async function requireVenue({ pathname }: { pathname: string }) {
  if (pathname === "/create") return;
  try {
    const venue = await venueApi.getVenue();
    if (venue === null) throw redirect({ to: "/create" });
  } catch (e) {
    if (e instanceof Response || (e as any)?.isRedirect) throw e;
    if (isApiError(e) && e.status === 401) throw redirect({ to: "/login" });
    throw e;
  }
}
