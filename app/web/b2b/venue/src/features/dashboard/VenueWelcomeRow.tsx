import { useVenueOverview } from "./hooks";
import { Skeleton } from "@/components/ui/skeleton";
import { ProfileHealthCard } from "@/features/dashboard";

export function VenueWelcomeRow() {
  const { data, isLoading } = useVenueOverview();

  if (isLoading || !data) {
    return (
      <>
        <div className="col-span-12 flex flex-col gap-2 md:col-span-7">
          <Skeleton className="h-7 w-72" />
          <Skeleton className="h-4 w-48" />
        </div>
        <Skeleton className="col-span-12 h-44 md:col-span-5" />
      </>
    );
  }

  return (
    <>
      <div className="col-span-12 flex flex-col justify-center gap-1 md:col-span-7">
        <h1 className="text-2xl font-semibold">Welcome back, {data.venueName}</h1>
        <p className="text-muted-foreground text-sm">
          {data.reviewSummary.totalReviews > 0
            ? `${data.reviewSummary.averageRating?.toFixed(1)} ★ from ${data.reviewSummary.totalReviews} reviews`
            : "No reviews yet — once your first concert settles, reviews land here."}
        </p>
      </div>
      <div className="col-span-12 md:col-span-5">
        <ProfileHealthCard health={data.profileHealth} />
      </div>
    </>
  );
}
