import "../shared/global.css";
import {
  useFonts,
  Geist_400Regular,
  Geist_500Medium,
  Geist_600SemiBold,
  Geist_700Bold,
} from "@expo-google-fonts/geist";
import { PortalHost } from "@rn-primitives/portal";
import { AppProviders } from "shared/providers/AppProviders";
import { RootNavigator } from "./src/navigation/RootNavigator";

export default function App() {
  const [fontsLoaded] = useFonts({
    Geist_400Regular,
    Geist_500Medium,
    Geist_600SemiBold,
    Geist_700Bold,
  });

  if (!fontsLoaded) return null;

  return (
    <AppProviders>
      <RootNavigator />
      <PortalHost />
    </AppProviders>
  );
}
