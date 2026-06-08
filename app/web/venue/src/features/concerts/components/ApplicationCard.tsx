import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useImageUrl } from "@concertable/shared/hooks";
import { useNavigate } from "@tanstack/react-router";
import dayjs from "dayjs";
import type { Application } from "@/features/concerts";

interface Props {
  application: Application;
  onDeny?: (applicationId: number) => void;
}

export function ApplicationCard({ application, onDeny }: Readonly<Props>) {
  const navigate = useNavigate();
  const { artist, opportunity, status } = application;
  const { data: avatarSrc } = useImageUrl(artist.avatar);

  function handleAccept() {
    navigate({
      to: "/applications/$applicationId/accept",
      params: { applicationId: application.id },
    });
  }

  return (
    <div
      className="border-border bg-card space-y-3 rounded-xl border p-4"
      data-testid={`application-${application.id}`}
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          {avatarSrc && (
            <img
              src={avatarSrc}
              alt={artist.name}
              className="h-10 w-10 rounded-full object-cover"
            />
          )}
          <div className="space-y-0.5">
            <p className="font-medium">{artist.name}</p>
            <p className="text-muted-foreground text-sm">
              {dayjs(opportunity.startDate).format("D MMM YYYY")} â€”{" "}
              {dayjs(opportunity.endDate).format("D MMM YYYY")}
            </p>
          </div>
        </div>

        <div className="flex shrink-0 items-center gap-2">
          <Badge variant="outline">{status}</Badge>
          {status === "Pending" && (
            <>
              <Button size="sm" onClick={handleAccept} data-testid="accept">
                Accept
              </Button>
              {onDeny && (
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => onDeny(application.id)}
                >
                  Deny
                </Button>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
