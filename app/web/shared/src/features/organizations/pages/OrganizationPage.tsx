import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { useOrganizationQuery } from "../hooks/useOrganizationQuery";
import { useUpdateOrganizationMutation } from "../hooks/useUpdateOrganizationMutation";
import type { Organization } from "../types";

interface OrganizationPageProps {
  title: string;
  description: string;
}

export function OrganizationPage({ title, description }: OrganizationPageProps) {
  const { data: organization, isLoading } = useOrganizationQuery();

  return (
    <div className="max-w-lg space-y-8">
      <div>
        <h2 className="text-lg font-semibold">{title}</h2>
        <p className="text-muted-foreground text-sm">{description}</p>
      </div>

      <Separator />

      {isLoading ? (
        <div className="text-muted-foreground size-5 animate-spin rounded-full border-2 border-current border-t-transparent" />
      ) : organization ? (
        <OrganizationForm organization={organization} />
      ) : (
        <p className="text-muted-foreground text-sm">
          No organization found for your account.
        </p>
      )}
    </div>
  );
}

function OrganizationForm({ organization }: { organization: Organization }) {
  const { mutate: save, isPending } = useUpdateOrganizationMutation();

  const compliance = organization.compliance;
  const [legalName, setLegalName] = useState(organization.legalName);
  const [vatRegistered, setVatRegistered] = useState(
    compliance?.vatRegistered ?? false,
  );
  const [vatNumber, setVatNumber] = useState(compliance?.vatNumber ?? "");
  const [sellerIdentifier, setSellerIdentifier] = useState(
    compliance?.sellerIdentifier ?? "",
  );
  const [line1, setLine1] = useState(compliance?.registeredAddress.line1 ?? "");
  const [line2, setLine2] = useState(compliance?.registeredAddress.line2 ?? "");
  const [city, setCity] = useState(compliance?.registeredAddress.city ?? "");
  const [postcode, setPostcode] = useState(
    compliance?.registeredAddress.postcode ?? "",
  );
  const [country, setCountry] = useState(
    compliance?.registeredAddress.country ?? "United Kingdom",
  );
  const [bankReference, setBankReference] = useState(
    compliance?.bankReference ?? "",
  );

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    save(
      {
        legalName,
        compliance: {
          vatRegistered,
          vatNumber: vatRegistered ? vatNumber : null,
          sellerIdentifier,
          registeredAddress: {
            line1,
            line2: line2 || null,
            city,
            postcode,
            country,
          },
          bankReference,
        },
      },
      {
        onSuccess: () => toast.success("Details saved"),
        onError: () =>
          toast.error("Couldn't save — check the details and try again"),
      },
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-8">
      <div className="space-y-4">
        <h3 className="font-medium">Legal identity</h3>
        <div className="space-y-1">
          <Label htmlFor="legalName">Legal name</Label>
          <Input
            id="legalName"
            value={legalName}
            onChange={(e) => setLegalName(e.target.value)}
            required
            maxLength={200}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sellerIdentifier">Company / seller identifier</Label>
          <Input
            id="sellerIdentifier"
            value={sellerIdentifier}
            onChange={(e) => setSellerIdentifier(e.target.value)}
            required
            maxLength={50}
          />
          <p className="text-muted-foreground text-xs">
            Companies House number, or your UTR if you're a sole trader.
          </p>
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">VAT</h3>
        <div className="flex items-center gap-2">
          <Checkbox
            id="vatRegistered"
            checked={vatRegistered}
            onCheckedChange={(checked) => setVatRegistered(checked === true)}
          />
          <Label htmlFor="vatRegistered">VAT registered</Label>
        </div>
        {vatRegistered && (
          <div className="space-y-1">
            <Label htmlFor="vatNumber">VAT number</Label>
            <Input
              id="vatNumber"
              value={vatNumber}
              onChange={(e) => setVatNumber(e.target.value)}
              required
              maxLength={20}
              placeholder="GB123456789"
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
            value={line1}
            onChange={(e) => setLine1(e.target.value)}
            required
            maxLength={200}
          />
        </div>
        <div className="space-y-1">
          <Label htmlFor="line2">Address line 2</Label>
          <Input
            id="line2"
            value={line2}
            onChange={(e) => setLine2(e.target.value)}
            maxLength={200}
          />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1">
            <Label htmlFor="city">City</Label>
            <Input
              id="city"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              required
              maxLength={100}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="postcode">Postcode</Label>
            <Input
              id="postcode"
              value={postcode}
              onChange={(e) => setPostcode(e.target.value)}
              required
              maxLength={20}
            />
          </div>
        </div>
        <div className="space-y-1">
          <Label htmlFor="country">Country</Label>
          <Input
            id="country"
            value={country}
            onChange={(e) => setCountry(e.target.value)}
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
            value={bankReference}
            onChange={(e) => setBankReference(e.target.value)}
            required
            maxLength={50}
            placeholder="IBAN or account reference"
          />
        </div>
      </div>

      <Button type="submit" disabled={isPending}>
        {isPending ? "Saving..." : "Save details"}
      </Button>
    </form>
  );
}
