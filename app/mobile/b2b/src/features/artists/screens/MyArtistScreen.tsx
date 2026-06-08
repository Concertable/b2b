import { useLayoutEffect } from "react";
import { View } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { notify } from "@/lib/toast";
import {
  useMyArtist,
  useArtistStore,
} from "@concertable/shared/features/artists";
import { EditableProvider } from "@concertable/shared/providers";
import { Screen } from "@/components/ui/Screen";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorState } from "@/components/ui/ErrorState";
import { ConfigBar } from "@/components/ConfigBar";
import { ArtistDetails } from "shared/features/artists/components/ArtistDetails";

export function MyArtistScreen() {
  const nav = useNavigation();

  const {
    artist,
    isLoading,
    isError,
    editMode,
    isDirty,
    isSaving,
    save,
    toggleEdit,
    resetDraft,
  } = useMyArtist({
    onSuccess: () => notify("Artist saved!", "success"),
    onError: () => notify("Failed to save artist.", "error"),
  });

  const draft = useArtistStore((s) => s.draft);
  const setName = useArtistStore((s) => s.setName);
  const setAbout = useArtistStore((s) => s.setAbout);
  const setBanner = useArtistStore((s) => s.setBanner);
  const setAvatar = useArtistStore((s) => s.setAvatar);

  useLayoutEffect(() => {
    nav.setOptions({
      headerRight: () => (
        <ConfigBar
          editMode={editMode}
          isDirty={isDirty}
          isSaving={isSaving}
          onToggleEdit={toggleEdit}
          onSave={save}
          onCancel={resetDraft}
        />
      ),
    });
  }, [nav, editMode, isDirty, isSaving, toggleEdit, save, resetDraft]);

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
