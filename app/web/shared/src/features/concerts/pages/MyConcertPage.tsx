import type { ReactNode } from "react";
import { ConfigBar } from "@/components/ConfigBar";
import { Button } from "@/components/ui/button";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsPageSkeleton } from "@/components/skeletons/DetailsPageSkeleton";
import type { Concert } from "@concertable/shared/features/concerts/types";
import { useMyConcert } from "../hooks/useMyConcert";
import { useDownloadAgreement } from "../hooks/useDownloadAgreement";
import { useConcertStore } from "../store/useConcertStore";
import { ConcertDetails } from "@/features/concerts";

interface Props {
  id: number;
  // Slot for app-specific manager actions (e.g. the venue's cancel-booking button).
  // The artist app renders none — cancelling a booking is a venue-only decision.
  renderActions?: (concert: Concert) => ReactNode;
}

export function MyConcertPage({ id, renderActions }: Readonly<Props>) {
  const { concert, isDirty, isSaving, save, resetDraft, toggleEdit, editMode } =
    useMyConcert(id);

  const draft = useConcertStore((state) => state.draft);
  const setName = useConcertStore((state) => state.setName);
  const setAbout = useConcertStore((state) => state.setAbout);
  const downloadAgreement = useDownloadAgreement();

  if (!concert) return <DetailsPageSkeleton sections={4} />;

  const display = draft ?? concert;

  return (
    <div>
      <ConfigBar
        editMode={editMode}
        isDirty={isDirty}
        isSaving={isSaving}
        onToggleEdit={toggleEdit}
        onSave={() => save()}
        onCancel={resetDraft}
      />
      {renderActions?.(concert)}
      {concert.actions?.agreement && (
        <div className="mx-auto max-w-5xl px-4 pt-4">
          <Button
            variant="outline"
            size="sm"
            onClick={() => downloadAgreement.mutate(concert.id)}
            disabled={downloadAgreement.isPending}
            data-testid="download-agreement"
          >
            Booking agreement
          </Button>
        </div>
      )}
      <EditableProvider editMode={editMode}>
        <ConcertDetails
          concert={display}
          onNameChange={setName}
          onAboutChange={setAbout}
        />
      </EditableProvider>
    </div>
  );
}
