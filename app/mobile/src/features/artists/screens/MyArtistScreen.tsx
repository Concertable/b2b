import { useLayoutEffect } from "react";
import { View } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { notify } from "@concertable/mobile/lib/toast";
import { useMyArtist } from "@concertable/b2b/features/artists";
import { EditableProvider } from "@concertable/shared/providers";
import { Screen } from "@concertable/mobile/components/ui/Screen";
import { Skeleton } from "@concertable/mobile/components/ui/skeleton";
import { ErrorState } from "@concertable/mobile/components/ui/ErrorState";
import { ConfigBar } from "@concertable/mobile/components/ConfigBar";
import { ArtistDetails } from "@concertable/mobile/features/artists/components/ArtistDetails";
import { useActiveTenantId } from "../../tenant/ActiveTenantContext";

export function MyArtistScreen() {
  const nav = useNavigation();
  const tenantId = useActiveTenantId();

  const {
    artist,
    draft,
    isLoading,
    isError,
    editMode,
    isDirty,
    isSaving,
    canSave,
    saveError,
    save,
    toggleEdit,
    resetDraft,
    setName,
    setAbout,
    setBanner,
    setAvatar,
  } = useMyArtist(tenantId, {
    onSuccess: () => notify("Artist saved!", "success"),
  });

  useLayoutEffect(() => {
    nav.setOptions({
      headerRight: () => (
        <ConfigBar
          editMode={editMode}
          isDirty={isDirty}
          isSaving={isSaving}
          canSave={canSave}
          error={saveError}
          onToggleEdit={toggleEdit}
          onSave={save}
          onCancel={resetDraft}
        />
      ),
    });
  }, [
    nav,
    editMode,
    isDirty,
    isSaving,
    canSave,
    saveError,
    toggleEdit,
    save,
    resetDraft,
  ]);

  if (isLoading) {
    return (
      <View className="flex-1 bg-background">
        <Skeleton className="w-full h-[240px] rounded-none" />
        <View className="p-4 gap-4">
          <Skeleton className="w-[70%] h-6" />
          <Skeleton className="w-full h-24" />
        </View>
      </View>
    );
  }

  if (isError || !artist) {
    return (
      <View className="flex-1 bg-background">
        <ErrorState message="Failed to load artist." />
      </View>
    );
  }

  const display = draft ?? artist;

  return (
    <Screen scroll padded={false}>
      <EditableProvider editMode={editMode}>
        <ArtistDetails
          artist={display}
          onNameChange={setName}
          onAboutChange={setAbout}
          onBannerChange={setBanner}
          onAvatarChange={setAvatar}
        />
      </EditableProvider>
    </Screen>
  );
}
