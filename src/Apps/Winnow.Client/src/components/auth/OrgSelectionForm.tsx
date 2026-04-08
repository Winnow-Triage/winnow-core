import { Button } from "@/components/ui/button";
import type { Organization } from "@/types";

interface OrgSelectionFormProps {
  availableOrgs: Pick<Organization, "id" | "name">[];
  selectedOrgId: string | null;
  setSelectedOrgId: (id: string) => void;
  isLoading: boolean;
  onBack: () => void;
}

export function OrgSelectionForm({
  availableOrgs,
  selectedOrgId,
  setSelectedOrgId,
  isLoading,
  onBack,
}: OrgSelectionFormProps) {
  return (
    <div className="space-y-4 animate-in fade-in slide-in-from-bottom-2 duration-300 w-full">
      <div className="text-sm font-medium">
        Select an organization to continue:
      </div>
      <div className="grid gap-2">
        {availableOrgs.map((org) => (
          <Button
            key={org.id}
            variant={selectedOrgId === org.id ? "default" : "outline"}
            className="w-full justify-start text-left font-normal"
            onClick={() => setSelectedOrgId(org.id)}
            type="button"
            disabled={isLoading}
          >
            <div className="flex flex-col items-start">
              <span>{org.name}</span>
            </div>
          </Button>
        ))}
      </div>
      
      <Button
        className="w-full mt-4"
        type="submit"
        disabled={!selectedOrgId || isLoading}
      >
        {isLoading ? "Signing in..." : "Continue to Dashboard"}
      </Button>

      <Button
        variant="ghost"
        className="w-full text-xs text-muted-foreground hover:text-primary transition-colors"
        onClick={onBack}
        type="button"
        disabled={isLoading}
      >
        Back to login
      </Button>
    </div>
  );
}
