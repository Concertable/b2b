import type { ReactNode } from "react";
import { ConfigBar } from "@concertable/web/shared/components/ConfigBar";
import { Button } from "@concertable/web/shared/components/ui/button";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsPageSkeleton } from "@concertable/web/shared/components/skeletons/DetailsPageSkeleton";
import type { MyConcert } from "../types";
import { useMyConcert } from "../hooks/useMyConcert";
import { useDownloadContractMutation } from "../hooks/useDownloadContractMutation";
import { useConcertStore } from "../store/useConcertStore";
import { ConcertDetails } from "@concertable/web/shared/features/concerts";

interface Props {
  id: number;
  // Slot for app-specific manager actions (e.g. the venue's cancel-booking button).
  // The artist app renders none — cancelling a booking is a venue-only decision.
  renderActions?: (concert: MyConcert) => ReactNode;
}

export function MyConcertPage({ id, renderActions }: Readonly<Props>) {
  const {
    concert,
    isDirty,
    isSaving,
    canSave,
    saveError,
    save,
    resetDraft,
    toggleEdit,
    editMode,
  } = useMyConcert(id);

  const draft = useConcertStore((state) => state.draft);
  const setName = useConcertStore((state) => state.setName);
  const setAbout = useConcertStore((state) => state.setAbout);
  const downloadContract = useDownloadContractMutation();

  if (!concert) return <DetailsPageSkeleton sections={4} />;

  const display = draft ?? concert;
  const actions = renderActions?.(concert);

  return (
    <div>
      <ConfigBar
        editMode={editMode}
        isDirty={isDirty}
        isSaving={isSaving}
        canSave={canSave}
        error={saveError}
        onToggleEdit={toggleEdit}
        onSave={() => save()}
        onCancel={resetDraft}
        actions={
          <>
            {concert.actions?.contract && (
              <Button
                variant="outline"
                onClick={() => downloadContract.mutate(concert.id)}
                disabled={downloadContract.isPending}
                data-testid="download-contract"
              >
                Contract
              </Button>
            )}
            {actions}
          </>
        }
      />
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
