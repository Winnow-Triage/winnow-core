import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import type { Organization, BillingStatus } from "@/types";

interface BillingSettingsProps {
  organization: Organization | undefined;
  billingStatus: BillingStatus | undefined;
  isBillingLoading: boolean;
  isCheckingOut: string | null;
  handleCheckout: (tier: string) => void;
  getButtonText: (
    tier: string,
    currentTier: string | undefined,
    checkingOutTier: string | null,
  ) => string;
}

export function BillingSettings({
  organization,
  billingStatus,
  isBillingLoading,
  isCheckingOut,
  handleCheckout,
  getButtonText,
}: BillingSettingsProps) {
  const subscriptionTier = organization?.subscriptionTier || "Free";

  return (
    <>
      <Card className="mb-6">
        <CardHeader>
          <CardTitle>Subscription Plan</CardTitle>
          <CardDescription>
            You are currently on the{" "}
            <strong>{subscriptionTier}</strong> plan.
          </CardDescription>
        </CardHeader>
        {billingStatus && !isBillingLoading && (
          <CardContent className="pb-0 border-t pt-4">
            <div className="space-y-4 mb-4">
              <div className="flex justify-between items-end mb-1">
                <div>
                  <div className="text-sm font-medium">Monthly Ingestion</div>
                  <div className="text-xs text-muted-foreground">
                    Reports collected this billing cycle
                  </div>
                </div>
                <div className="text-right">
                  <span className="text-xl font-bold">
                    {billingStatus.reportsUsedThisMonth}
                  </span>
                  <span className="text-sm text-muted-foreground ml-1">
                    / {billingStatus.reportLimit || "∞"}
                  </span>
                </div>
              </div>
              <Progress
                value={
                  billingStatus.reportLimit
                    ? (billingStatus.reportsUsedThisMonth /
                        billingStatus.reportLimit) *
                      100
                    : 100
                }
                className={`h-2 ${billingStatus.reportLimit && billingStatus.reportsUsedThisMonth >= billingStatus.reportLimit ? "[&>div]:bg-destructive" : "[&>div]:bg-blue-500"}`}
              />

              {["Pro", "Enterprise"].includes(subscriptionTier) ? (
                <div className="pt-4 mt-4 border-t space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">
                      AI Triage Tokens
                    </span>
                    <span className="font-medium text-emerald-600 dark:text-emerald-400">
                      Unlimited
                    </span>
                  </div>
                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span>Active limits</span>
                    <span>No cap on automated triage summaries.</span>
                  </div>
                </div>
              ) : ["Starter", "Free"].includes(subscriptionTier) ? (
                <div className="pt-4 mt-4 border-t space-y-2">
                  <div className="flex justify-between items-end mb-1">
                    <div>
                      <div className="text-sm font-medium">
                        AI Summaries
                      </div>
                      <div className="text-xs text-muted-foreground">
                        Monthly automated reports constraint
                      </div>
                    </div>
                    <div className="text-right">
                      <span className="font-bold">
                        {billingStatus.currentMonthSummaries}
                      </span>
                      <span className="text-xs text-muted-foreground ml-1">
                        / {billingStatus.monthlySummaryLimit || "-"}
                      </span>
                    </div>
                  </div>
                  <Progress
                    value={
                      billingStatus.monthlySummaryLimit
                        ? (billingStatus.currentMonthSummaries /
                            billingStatus.monthlySummaryLimit) *
                          100
                        : 0
                    }
                    className={`h-1.5 ${billingStatus.monthlySummaryLimit && billingStatus.currentMonthSummaries >= billingStatus.monthlySummaryLimit ? "[&>div]:bg-amber-500" : "[&>div]:bg-indigo-500"}`}
                  />
                  <div className="flex justify-between text-[10px] text-muted-foreground font-medium uppercase tracking-wider">
                    <span>
                      {billingStatus.currentMonthSummaries} generated
                    </span>
                    <span>{billingStatus.monthlySummaryLimit} Limit</span>
                  </div>
                </div>
              ) : (
                <div className="flex justify-between text-xs text-muted-foreground">
                  <span>No AI Summaries generated.</span>
                  {subscriptionTier === "Free" ? (
                    <span>
                      Upgrade to Starter or Pro to enable AI triage.
                    </span>
                  ) : (
                    <span>
                      AI Triage plan limit reached or disabled.
                    </span>
                  )}
                </div>
              )}
            </div>
          </CardContent>
        )}
        {billingStatus?.reportLimit !== null &&
          billingStatus &&
          billingStatus.reportsUsedThisMonth >=
          billingStatus.reportLimit && (
            <CardFooter className="bg-destructive/10 text-destructive text-sm p-4 rounded-b-3xl border-t border-destructive/20">
              You have reached your monthly ingestion limit. New reports
              will be rejected until you upgrade your plan or until the
              next billing cycle.
            </CardFooter>
          )}
      </Card>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
        <Card className="flex flex-col h-full">
          <CardHeader>
            <CardTitle>Cloud Free</CardTitle>
            <CardDescription>$0 / month</CardDescription>
          </CardHeader>
          <CardContent className="flex-1">
            <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
              <li>Fully Managed Hosting</li>
              <li>Up to 1,000 reports / mo</li>
              <li>1-Day Log Retention</li>
              <li>Community Support</li>
            </ul>
          </CardContent>
          <CardFooter>
            <Button
              className="w-full"
              variant={
                subscriptionTier === "Free" ? "secondary" : "outline"
              }
              onClick={() => handleCheckout("Free")}
              disabled={
                isCheckingOut !== null || subscriptionTier === "Free"
              }
            >
              {getButtonText("Free", subscriptionTier, isCheckingOut)}
            </Button>
          </CardFooter>
        </Card>
        <Card className="flex flex-col h-full">
          <CardHeader>
            <CardTitle>Starter</CardTitle>
            <CardDescription>$15 / month</CardDescription>
          </CardHeader>
          <CardContent className="flex-1">
            <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
              <li>Up to 3 members</li>
              <li>Basic reporting</li>
              <li>Community support</li>
            </ul>
          </CardContent>
          <CardFooter>
            <Button
              className="w-full"
              variant={
                subscriptionTier === "Starter" ? "secondary" : "default"
              }
              onClick={() => handleCheckout("Starter")}
              disabled={
                isCheckingOut !== null || subscriptionTier === "Starter"
              }
            >
              {getButtonText("Starter", subscriptionTier, isCheckingOut)}
            </Button>
          </CardFooter>
        </Card>

        <Card
          className={`relative flex flex-col h-full ${!["Pro", "Enterprise"].includes(subscriptionTier) ? "border-primary" : ""}`}
        >
          {!["Pro", "Enterprise"].includes(subscriptionTier) && (
            <div className="absolute -top-3 left-1/2 -translate-x-1/2 px-3 py-1 bg-primary text-primary-foreground text-xs font-semibold rounded-full">
              Recommended
            </div>
          )}
          <CardHeader>
            <CardTitle>Pro</CardTitle>
            <CardDescription>$79 / month</CardDescription>
          </CardHeader>
          <CardContent className="flex-1">
            <ul className="list-disc pl-4 space-y-1 text-sm text-muted-foreground">
              <li>Unlimited members</li>
              <li>Advanced reporting & AI</li>
              <li>Priority support</li>
            </ul>
          </CardContent>
          <CardFooter>
            <Button
              className="w-full"
              variant={subscriptionTier === "Pro" ? "secondary" : "default"}
              onClick={() => handleCheckout("Pro")}
              disabled={
                isCheckingOut !== null || subscriptionTier === "Pro"
              }
            >
              {getButtonText("Pro", subscriptionTier, isCheckingOut)}
            </Button>
          </CardFooter>
        </Card>

        <Card className="flex flex-col h-full bg-zinc-950 text-zinc-50 border-zinc-800 dark:bg-zinc-900">
          <CardHeader>
            <CardTitle className="text-zinc-50">Enterprise</CardTitle>
            <CardDescription className="text-zinc-400">
              Custom Pricing
            </CardDescription>
          </CardHeader>
          <CardContent className="flex-1">
            <ul className="list-disc pl-4 space-y-1 text-sm text-zinc-400 marker:text-zinc-600">
              <li>Dedicated tenant infrastructure</li>
              <li>Custom integrations</li>
              <li>SLA & Account Manager</li>
            </ul>
          </CardContent>
          <CardFooter>
            <Button
              className={`w-full ${subscriptionTier === "Enterprise" ? "bg-zinc-800 text-zinc-300 hover:bg-zinc-800" : "bg-white text-zinc-950 hover:bg-zinc-200"}`}
              onClick={() =>
              (window.location.href =
                "mailto:sales@winnowtriage.com?subject=Enterprise%20Plan%20Inquiry")
              }
              disabled={subscriptionTier === "Enterprise"}
            >
                {subscriptionTier === "Enterprise"
                ? "Current Plan"
                : "Contact Sales / Upgrade"}
            </Button>
          </CardFooter>
        </Card>
      </div>
    </>
  );
}
