import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import {
  useOrganization,
  type OrganizationBuffer,
} from "../hooks/useOrganization";
import { taxFormLabels } from "../taxFormLabels";
import type { Organization } from "../types";

function initialBuffer(organization: Organization): OrganizationBuffer {
  const tax = organization.taxCompliance;
  return {
    legalName: organization.legalName,
    vatRegistered: tax?.vatNumber != null,
    vatNumber: tax?.vatNumber ?? "",
    sellerIdentifier: tax?.sellerIdentifier ?? "",
    line1: tax?.registeredAddress.line1 ?? "",
    line2: tax?.registeredAddress.line2 ?? "",
    city: tax?.registeredAddress.city ?? "",
    postcode: tax?.registeredAddress.postcode ?? "",
    country: tax?.registeredAddress.country ?? "United Kingdom",
    bankReference: tax?.bankReference ?? "",
    holdsMusicLicence: tax?.holdsMusicLicence ?? false,
  };
}

export function OrganizationForm({
  organization,
}: Readonly<{ organization: Organization }>) {
  const { isSaving, save } = useOrganization();
  const [buffer, setBuffer] = useState(() => initialBuffer(organization));
  const [error, setError] = useState<string | null>(null);

  function set<K extends keyof OrganizationBuffer>(
    key: K,
    value: OrganizationBuffer[K],
  ) {
    setBuffer((current) => ({ ...current, [key]: value }));
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    const parsed = save(buffer);
    setError(parsed.success ? null : parsed.error.issues[0].message);
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      <div className="space-y-4">
        <h3 className="font-medium">Legal identity</h3>
        <div className="space-y-1">
          <Label htmlFor="legalName">Legal name</Label>
          <Input
            id="legalName"
            value={buffer.legalName}
            onChange={(e) => set("legalName", e.target.value)}
            required
            maxLength={200}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sellerIdentifier">{taxFormLabels.sellerIdentifierLabel}</Label>
          <Input
            id="sellerIdentifier"
            value={buffer.sellerIdentifier}
            onChange={(e) => set("sellerIdentifier", e.target.value)}
            required
            maxLength={50}
          />
          <p className="text-muted-foreground text-xs">
            {taxFormLabels.sellerIdentifierHint}
          </p>
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">VAT</h3>
        <div className="flex items-center gap-2">
          <Checkbox
            id="vatRegistered"
            checked={buffer.vatRegistered}
            onCheckedChange={(checked) => set("vatRegistered", checked === true)}
          />
          <Label htmlFor="vatRegistered">VAT registered</Label>
        </div>
        {buffer.vatRegistered && (
          <div className="space-y-1">
            <Label htmlFor="vatNumber">{taxFormLabels.vatLabel}</Label>
            <Input
              id="vatNumber"
              value={buffer.vatNumber}
              onChange={(e) => set("vatNumber", e.target.value)}
              required
              maxLength={20}
              placeholder={taxFormLabels.vatNumberPlaceholder}
            />
          </div>
        )}
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Registered address</h3>
        <div className="space-y-1">
          <Label htmlFor="line1">Address line 1</Label>
          <Input
            id="line1"
            value={buffer.line1}
            onChange={(e) => set("line1", e.target.value)}
            required
            maxLength={200}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="line2">Address line 2</Label>
          <Input
            id="line2"
            value={buffer.line2}
            onChange={(e) => set("line2", e.target.value)}
            maxLength={200}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1">
            <Label htmlFor="city">City</Label>
            <Input
              id="city"
              value={buffer.city}
              onChange={(e) => set("city", e.target.value)}
              required
              maxLength={100}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="postcode">Postcode</Label>
            <Input
              id="postcode"
              value={buffer.postcode}
              onChange={(e) => set("postcode", e.target.value)}
              required
              maxLength={20}
            />
          </div>
        </div>
        <div className="space-y-1">
          <Label htmlFor="country">Country</Label>
          <Input
            id="country"
            value={buffer.country}
            onChange={(e) => set("country", e.target.value)}
            required
            maxLength={100}
          />
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Payout bank reference</h3>
        <div className="space-y-1">
          <Label htmlFor="bankReference">Bank reference</Label>
          <Input
            id="bankReference"
            value={buffer.bankReference}
            onChange={(e) => set("bankReference", e.target.value)}
            required
            maxLength={50}
            placeholder="IBAN or account reference"
          />
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Music licence</h3>
        <div className="flex items-center gap-2">
          <Checkbox
            id="holdsMusicLicence"
            checked={buffer.holdsMusicLicence}
            onCheckedChange={(checked) => set("holdsMusicLicence", checked === true)}
          />
          <Label htmlFor="holdsMusicLicence">{taxFormLabels.musicLicenceLabel}</Label>
        </div>
        <p className="text-muted-foreground text-xs">
          {taxFormLabels.musicLicenceHint}
        </p>
      </div>

      {error && (
        <p className="text-destructive text-xs" data-testid="organization-error">
          {error}
        </p>
      )}

      <Button type="submit" disabled={isSaving}>
        {isSaving ? "Saving..." : "Save details"}
      </Button>
    </form>
  );
}
