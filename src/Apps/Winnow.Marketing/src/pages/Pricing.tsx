
import { Check, Users } from 'lucide-react';
import type { ReactNode } from 'react';

interface PricingFeatureProps {
    children: ReactNode;
}

function PricingFeature({ children }: PricingFeatureProps) {
    return (
        <li className="flex items-center text-muted-foreground text-sm">
            <Check className="mr-2 h-4 w-4 text-primary" />
            {children}
        </li>
    );
}

export function Pricing() {
    return (
        <div className="bg-slate-50 dark:bg-slate-950 py-24 min-h-screen">
            <div className="container mx-auto px-4 md:px-6">
                <div className="text-center mb-16 space-y-4">
                    <h1 className="text-4xl font-bold tracking-tighter sm:text-5xl md:text-6xl text-slate-900 dark:text-slate-50">
                        Pricing that scales with your <span className="text-primary">bugs</span>.
                    </h1>
                    <p className="mx-auto max-w-[700px] text-lg text-muted-foreground md:text-xl">
                        Start for free, upgrade as you grow. No hidden fees for "extra seats" or "data ingestion."
                    </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 max-w-7xl mx-auto">

                    {/* Tier 1: Community */}
                    <div className="flex flex-col p-6 bg-white dark:bg-slate-900 rounded-xl shadow-lg border border-slate-200 dark:border-slate-800">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-slate-900 dark:text-slate-50">Community</h3>
                            <p className="text-sm text-muted-foreground">For hobbyists & self-hosters.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-slate-900 dark:text-slate-50">$0</span>
                            <span className="text-muted-foreground ml-2">/ forever</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Self-Hosted (Docker)</PricingFeature>
                            <PricingFeature>Community Support</PricingFeature>
                            <PricingFeature>Unlimited Projects</PricingFeature>
                            <PricingFeature>Bring your own Infra</PricingFeature>
                        </ul>
                        <a href="https://github.com/winnow-org/winnow" target="_blank" rel="noreferrer" className="w-full inline-flex items-center justify-center rounded-lg border border-slate-200 dark:border-slate-700 bg-transparent px-8 py-3 text-sm font-medium text-slate-900 dark:text-slate-50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                            <Users className="mr-2 h-4 w-4" /> View Source
                        </a>
                    </div>

                    {/* Tier 2: Cloud Starter */}
                    <div className="flex flex-col p-6 bg-white dark:bg-slate-900 rounded-xl shadow-lg border border-slate-200 dark:border-slate-800 relative overflow-hidden">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-slate-900 dark:text-slate-50">Cloud Starter</h3>
                            <p className="text-sm text-muted-foreground">For indie developers.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-slate-900 dark:text-slate-50">$15</span>
                            <span className="text-muted-foreground ml-2">/ month</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Fully Managed Hosting</PricingFeature>
                            <PricingFeature>Shared Database</PricingFeature>
                            <PricingFeature>7-Day Log Retention</PricingFeature>
                            <PricingFeature>Standard Email Support</PricingFeature>
                        </ul>
                        <a href="http://localhost:5173/signup?tier=starter" className="w-full inline-flex items-center justify-center rounded-lg bg-blue-600 px-8 py-3 text-sm font-medium text-white hover:bg-blue-700 transition-colors shadow-md">
                            Start Trial
                        </a>
                    </div>

                    {/* Tier 3: Cloud Pro */}
                    <div className="flex flex-col p-6 bg-white dark:bg-slate-900 rounded-xl shadow-2xl border-2 border-primary scale-105 z-10 relative">
                        <div className="absolute top-0 right-0 bg-primary text-white text-xs font-bold px-3 py-1 rounded-bl-lg uppercase">
                            Most Popular
                        </div>
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-slate-900 dark:text-slate-50">Cloud Pro</h3>
                            <p className="text-sm text-muted-foreground">For growing startups.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-slate-900 dark:text-slate-50">$79</span>
                            <span className="text-muted-foreground ml-2">/ month</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Everything in Starter</PricingFeature>
                            <PricingFeature>90-Day Log Retention</PricingFeature>
                            <PricingFeature>Increased Rate Limits</PricingFeature>
                            <PricingFeature>Priority Support</PricingFeature>
                        </ul>
                        <a href="http://localhost:5173/signup?tier=pro" className="w-full inline-flex items-center justify-center rounded-lg bg-gradient-to-r from-blue-600 to-indigo-600 px-8 py-3 text-sm font-medium text-white hover:from-blue-700 hover:to-indigo-700 transition-all shadow-lg shadow-blue-500/25">
                            Upgrade to Pro
                        </a>
                    </div>

                    {/* Tier 4: Dedicated */}
                    <div className="flex flex-col p-6 bg-slate-900 text-slate-50 rounded-xl shadow-lg border border-slate-700">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-white">Dedicated</h3>
                            <p className="text-sm text-slate-400">For enterprise compliance.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-white">$299</span>
                            <span className="text-slate-400 ml-2">/ month</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Isolated Database Instance</PricingFeature>
                            <PricingFeature>SSO (SAML/Okta)</PricingFeature>
                            <PricingFeature>1-Year+ Log Retention</PricingFeature>
                            <PricingFeature>Private Slack Channel</PricingFeature>
                        </ul>
                        <a href="mailto:sales@winnowtriage.com" className="w-full inline-flex items-center justify-center rounded-lg bg-slate-50 px-8 py-3 text-sm font-medium text-slate-900 hover:bg-slate-200 transition-colors">
                            Contact Sales
                        </a>
                    </div>

                </div>
            </div>
        </div>
    );
}
