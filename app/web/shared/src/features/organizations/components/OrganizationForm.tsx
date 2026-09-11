import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Button } from "@concertable/web/components/ui/button";
import { Checkbox } from "@concertable/web/components/ui/checkbox";
import { Input } from "@concertable/web/components/ui/input";
import { Label } from "@concertable/web/components/ui/label";
import { Separator } from "@concertable/web/components/ui/separator";
import { useOrganization } from "../hooks/useOrganization";
import { updateOrganizationRequestSchema } from "../schemas/updateOrganizationRequestSchema";
import { taxFormLabels } from "../taxFormLabels";
import {
  Organization,
  type OrganizationFormValues,
  type UpdateOrganizationRequest,
} from "../types";

interface FieldErrorProps {
  id: string;
  message?: string;
}

function FieldError({ id, message }: Readonly<FieldErrorProps>) {
  if (!message) return null;
  return (
    <p id={id} className="text-destructive text-xs">
      {message}
    </p>
  );
}

export function OrganizationForm({
  organization,
}: Readonly<{ organization: Organization }>) {
  const { isSaving, save } = useOrganization();
  const {
    control,
    register,
    handleSubmit,
    watch,
    formState: { errors, isValid },
  } = useForm<OrganizationFormValues, unknown, UpdateOrganizationRequest>({
    resolver: zodResolver(updateOrganizationRequestSchema),
    defaultValues: Organization.toFormValues(organization),
    mode: "onChange",
  });
  const vatRegistered = watch("vatRegistered");

  return (
    <form onSubmit={handleSubmit(save)} className="space-y-8">
      <div className="space-y-4">
        <h3 className="font-medium">Legal identity</h3>
        <div className="space-y-1">
          <Label htmlFor="legalName">Legal name</Label>
          <Input
            id="legalName"
            aria-invalid={errors.legalName !== undefined}
            aria-describedby="legalName-error"
            {...register("legalName")}
          />
          <FieldError id="legalName-error" message={errors.legalName?.message} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="sellerIdentifier">
            {taxFormLabels.sellerIdentifierLabel}
          </Label>
          <Input
            id="sellerIdentifier"
            aria-invalid={errors.sellerIdentifier !== undefined}
            aria-describedby="sellerIdentifier-error"
            {...register("sellerIdentifier")}
          />
          <FieldError
            id="sellerIdentifier-error"
            message={errors.sellerIdentifier?.message}
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
          <Controller
            control={control}
            name="vatRegistered"
            render={({ field }) => (
              <Checkbox
                id="vatRegistered"
                checked={field.value}
                onCheckedChange={(checked) => field.onChange(checked === true)}
              />
            )}
          />
          <Label htmlFor="vatRegistered">VAT registered</Label>
        </div>
        {vatRegistered && (
          <div className="space-y-1">
            <Label htmlFor="vatNumber">{taxFormLabels.vatLabel}</Label>
            <Input
              id="vatNumber"
              placeholder={taxFormLabels.vatNumberPlaceholder}
              aria-invalid={errors.vatNumber !== undefined}
              aria-describedby="vatNumber-error"
              {...register("vatNumber")}
            />
            <FieldError id="vatNumber-error" message={errors.vatNumber?.message} />
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
            aria-invalid={errors.line1 !== undefined}
            aria-describedby="line1-error"
            {...register("line1")}
          />
          <FieldError id="line1-error" message={errors.line1?.message} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="line2">Address line 2</Label>
          <Input
            id="line2"
            aria-invalid={errors.line2 !== undefined}
            aria-describedby="line2-error"
            {...register("line2")}
          />
          <FieldError id="line2-error" message={errors.line2?.message} />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1">
            <Label htmlFor="city">City</Label>
            <Input
              id="city"
              aria-invalid={errors.city !== undefined}
              aria-describedby="city-error"
              {...register("city")}
            />
            <FieldError id="city-error" message={errors.city?.message} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="postcode">Postcode</Label>
            <Input
              id="postcode"
              aria-invalid={errors.postcode !== undefined}
              aria-describedby="postcode-error"
              {...register("postcode")}
            />
            <FieldError
              id="postcode-error"
              message={errors.postcode?.message}
            />
          </div>
        </div>
        <div className="space-y-1">
          <Label htmlFor="country">Country</Label>
          <Input
            id="country"
            aria-invalid={errors.country !== undefined}
            aria-describedby="country-error"
            {...register("country")}
          />
          <FieldError id="country-error" message={errors.country?.message} />
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Payout bank reference</h3>
        <div className="space-y-1">
          <Label htmlFor="bankReference">Bank reference</Label>
          <Input
            id="bankReference"
            placeholder="IBAN or account reference"
            aria-invalid={errors.bankReference !== undefined}
            aria-describedby="bankReference-error"
            {...register("bankReference")}
          />
          <FieldError
            id="bankReference-error"
            message={errors.bankReference?.message}
          />
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="font-medium">Music licence</h3>
        <div className="flex items-center gap-2">
          <Controller
            control={control}
            name="holdsMusicLicence"
            render={({ field }) => (
              <Checkbox
                id="holdsMusicLicence"
                checked={field.value}
                onCheckedChange={(checked) => field.onChange(checked === true)}
              />
            )}
          />
          <Label htmlFor="holdsMusicLicence">
            {taxFormLabels.musicLicenceLabel}
          </Label>
        </div>
        <p className="text-muted-foreground text-xs">
          {taxFormLabels.musicLicenceHint}
        </p>
      </div>

      <Button type="submit" disabled={isSaving || !isValid}>
        {isSaving ? "Saving..." : "Save details"}
      </Button>
    </form>
  );
}
