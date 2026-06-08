import { redirect } from "@tanstack/react-router";
import { isAxiosError } from "axios";
import venueApi from "@concertable/shared/features/venues/api/venueApi";

export async function requireVenue({ pathname }: { pathname: string }) {
  if (pathname === "/create") return;
  try {
    const venue = await venueApi.getMyVenue();
    if (venue === null) throw redirect({ to: "/create" });
  } catch (e) {
    if (e instanceof Response || (e as any)?.isRedirect) throw e;
    if (isAxiosError(e) && e.response?.status === 401) throw redirect({ to: "/login" });
    throw e;
  }
}
