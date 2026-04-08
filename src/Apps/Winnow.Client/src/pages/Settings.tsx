import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { useSearchParams } from "react-router-dom";
import { api } from "@/lib/api";
import type { Organization, BillingStatus } from "@/types";

import { OrganizationSettings } from "./settings/OrganizationSettings";
import { ToxicitySettings } from "./settings/ToxicitySettings";
import { MembersSettings } from "./settings/MembersSettings";
import { TeamsSettings } from "./settings/TeamsSettings";
import { BillingSettings } from "./settings/BillingSettings";
import { AISettings } from "./settings/AISettings";
import { NotificationSettings } from "./settings/NotificationSettings";

const tabTitles = {
  general: "General Settings",
  toxicity: "Toxicity Filtering",
  members: "Organization Directory",
  teams: "Teams",
  billing: "Subscription & Billing",
  notifications: "Notification Defaults",
  ai: "AI Configuration",
};

export default function Settings() {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = searchParams.get("tab") || "general";

  const {
    data: organization,
    isLoading,
    refetch,
  } = useQuery<Organization>({
    queryKey: ["organization"],
    queryFn: async () => {
      const { data } = await api.get("/organizations/current");
      return data;
    },
  });

  const { data: billingStatus, isLoading: isBillingLoading } =
    useQuery<BillingStatus>({
      queryKey: ["organization-billing-status"],
      queryFn: async () => {
        const { data } = await api.get("/organizations/current/billing-status");
        return data;
      },
    });

  const [isCheckingOut, setIsCheckingOut] = useState<string | null>(null);

  const handleCheckout = async (tier: string) => {
    setIsCheckingOut(tier);
    try {
      const { data } = await api.post("/billing/checkout", { tier });
      window.location.href = data.url;
    } catch (error) {
      console.error("Checkout error:", error);
    } finally {
      setIsCheckingOut(null);
    }
  };

  const getButtonText = (
    tier: string,
    currentTier: string | undefined,
    checkingOutTier: string | null,
  ) => {
    if (checkingOutTier === tier) return "Redirecting...";
    if (currentTier === tier) return "Current Plan";
    return "Upgrade";
  };


  useEffect(() => {
    document.title = `${tabTitles[activeTab as keyof typeof tabTitles] || "Settings"} - Winnow`;
  }, [activeTab]);

  return (
    <div className="flex-1 space-y-4 p-8 pt-6">
      <div className="flex items-center justify-between space-y-2 mb-6">
        <div>
          <h2 className="text-3xl font-bold tracking-tight">
            Workspace Settings
          </h2>
          <p className="text-muted-foreground">
            Manage your organization, members, and billing preferences.
          </p>
        </div>
      </div>

      <Tabs
        value={activeTab}
        onValueChange={(val) => setSearchParams({ tab: val })}
        className="space-y-4"
      >
        <TabsList className="bg-transparent border-b rounded-none w-full justify-start h-auto p-0 space-x-6">
          <TabsTrigger
            value="general"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            General
          </TabsTrigger>
          <TabsTrigger
            value="toxicity"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            Toxicity Filtering
          </TabsTrigger>
          <TabsTrigger
            value="members"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            Members
          </TabsTrigger>
          <TabsTrigger
            value="teams"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            Teams
          </TabsTrigger>
          <TabsTrigger
            value="billing"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            Billing
          </TabsTrigger>
          <TabsTrigger
            value="notifications"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            Notifications
          </TabsTrigger>
          <TabsTrigger
            value="ai"
            className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:bg-transparent px-0 py-3"
          >
            AI Setup
          </TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="mt-6 flex flex-col gap-6">
          <OrganizationSettings
            organization={organization}
            isLoading={isLoading}
            refetch={refetch}
          />
        </TabsContent>

        <TabsContent value="toxicity" className="mt-6 flex flex-col gap-6">
          <ToxicitySettings key={organization?.id} organization={organization} />
        </TabsContent>

        <TabsContent value="members" className="mt-6 flex flex-col gap-6">
          <MembersSettings organizationId={organization?.id} />
        </TabsContent>

        <TabsContent value="teams" className="mt-6 flex flex-col gap-6">
          <TeamsSettings organizationId={organization?.id} />
        </TabsContent>

        <TabsContent value="billing" className="mt-6 flex flex-col gap-6">
          <BillingSettings
            organization={organization}
            billingStatus={billingStatus}
            isBillingLoading={isBillingLoading}
            isCheckingOut={isCheckingOut}
            handleCheckout={handleCheckout}
            getButtonText={getButtonText}
          />
        </TabsContent>

        <TabsContent value="notifications" className="mt-6 flex flex-col gap-6">
          <NotificationSettings
            organization={organization}
            refetch={refetch}
          />
        </TabsContent>

        <TabsContent value="ai" className="mt-6 flex flex-col gap-6">
          <AISettings
            organization={organization}
            billingStatus={billingStatus}
            isBillingLoading={isBillingLoading}
            refetch={refetch}
          />
        </TabsContent>
      </Tabs>
    </div>
  );
}
