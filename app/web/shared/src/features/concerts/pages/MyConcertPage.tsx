import type { ReactNode } from "react";
import { ConfigBar } from "@concertable/web/components/ConfigBar";
import { Button } from "@concertable/web/components/ui/button";
import { EditableProvider } from "@concertable/shared/providers";
import { DetailsPageSkeleton } from "@concertable/web/components/skeletons/DetailsPageSkeleton";
import type { ConcertOperations } from "../types";
import { useMyConcert } from "../hooks/useMyConcert";
import { useDownloadContractMutation } from "../hooks/useDownloadContractMutation";
import { ConcertDetails } from "@concertable/web/features/concerts";

interface Props {
  id: number;
  renderActions?: (concert: ConcertOperations) => ReactNode;
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
    draft,
    setName,
    setAbout,
  } = useMyConcert(id);

  const downloadContract = useDownloadContractMutation();

  if (!concert) return <DetailsPageSkeleton sections={4} />;

  const display = { ...concert, ...draft };
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
        onSave={save}
        onCancel={resetDraft}
        actions={
          <>
            <Button
              variant="outline"
              onClick={() => downloadContract.mutate(concert.applicationId)}
              disabled={downloadContract.isPending}
              data-testid="download-contract"
            >
              Contract
            </Button>
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
