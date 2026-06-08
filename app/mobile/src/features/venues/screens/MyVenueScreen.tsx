import { useLayoutEffect } from "react";
import { View } from "react-native";
import { useNavigation } from "@react-navigation/native";
import { notify } from "@/lib/toast";
import {
  useMyVenue,
  useVenueStore,
} from "@concertable/shared/features/venues";
import { EditableProvider } from "@concertable/shared/providers";
import { Screen } from "@/components/ui/Screen";
import { Skeleton } from "@/components/ui/skeleton";
import { ErrorState } from "@/components/ui/ErrorState";
import { ConfigBar } from "@/components/ConfigBar";
import { VenueDetails } from "shared/features/venues/components/VenueDetails";

export function MyVenueScreen() {
  const nav = useNavigation();

  const {
    venue,
    isLoading,
    isError,
    editMode,
    isDirty,
    isSaving,
    save,
    toggleEdit,
    resetDraft,
  } = useMyVenue({
    onSuccess: () => notify("Venue saved!", "success"),
    onError: () => notify("Failed to save venue.", "error"),
  });

  const draft = useVenueStore((s) => s.draft);
  const setName = useVenueStore((s) => s.setName);
  const setAbout = useVenueStore((s) => s.setAbout);
  const setBanner = useVenueStore((s) => s.setBanner);
  const setAvatar = useVenueStore((s) => s.setAvatar);
  const setLocation = useVenueStore((s) => s.setLocation);

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

  if (isError || !venue) {
    return (
      <View className="flex-1 bg-background">
        <ErrorState message="Failed to load venue." />
      </View>
    );
  }

  const display = draft ?? venue;

  return (
    <Screen scroll padded={false}>
      <EditableProvider editMode={editMode}>
        <VenueDetails
          venue={display}
          onNameChange={setName}
          onAboutChange={setAbout}
          onBannerChange={setBanner}
          onAvatarChange={setAvatar}
          onLocationChange={setLocation}
        />
      </EditableProvider>
    </Screen>
  );
}
