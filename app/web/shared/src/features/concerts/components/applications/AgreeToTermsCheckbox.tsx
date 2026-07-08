import { Checkbox } from "@/components/ui/checkbox";

interface Props {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
}

export function AgreeToTermsCheckbox({ checked, onCheckedChange }: Readonly<Props>) {
  return (
    <label className="flex items-start gap-2 text-sm">
      <Checkbox
        checked={checked}
        onCheckedChange={(value) => onCheckedChange(value === true)}
        data-testid="agree-to-terms"
        className="mt-0.5"
      />
      <span>I agree to the contract terms.</span>
    </label>
  );
}
