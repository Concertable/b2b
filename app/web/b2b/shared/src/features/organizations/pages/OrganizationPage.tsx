import { Separator } from "@concertable/web/shared/components/ui/separator";
import { useOrganization } from "../hooks/useOrganization";
import { OrganizationForm } from "../components/OrganizationForm";

interface OrganizationPageProps {
  title: string;
  description: string;
}

export function OrganizationPage({ title, description }: OrganizationPageProps) {
  const { organization, isLoading } = useOrganization();

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
