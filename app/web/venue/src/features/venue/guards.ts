import { redirect } from "@tanstack/react-router";
import { isApiError } from "@concertable/shared/lib/apiError";
import venueApi from "@concertable/shared/features/venues/api/venueApi";

export async function requireVenue({ pathname }: { pathname: string }) {
  if (pathname === "/create") return;
  try {
    const venue = await venueApi.getMyVenue();
    if (venue === undefined) throw redirect({ to: "/create" });
  } catch (e) {
    if (e instanceof Response || (e as any)?.isRedirect) throw e;
    if (isApiError(e) && e.status === 401) throw redirect({ to: "/login" });
    throw e;
  }
}
