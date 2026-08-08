import "lucide-react-native";
import type { ColorValue } from "react-native";

declare module "lucide-react-native" {
  interface LucideProps {
    color?: ColorValue;
    className?: string;
    fill?: ColorValue;
    strokeWidth?: number | string;
  }
}
