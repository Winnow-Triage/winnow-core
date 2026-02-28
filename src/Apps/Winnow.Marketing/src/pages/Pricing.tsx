
import { Check, Users } from 'lucide-react';
import type { ReactNode } from 'react';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { SEOMeta } from '../components/SEOMeta';

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
        <div className="flex flex-col min-h-screen">
            <SEOMeta
                title="Pricing"
                description="Transparent pricing that scales with your growth. Start for free or upgrade to Pro for advanced clustering and enterprise-grade observability."
            />
            {/* Hero Section */}
            <Section variant="slate" border="bottom" padding="normal" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                    Pricing that scales <br className="hidden sm:block" />
                    with your <GradientText>bug reports.</GradientText>
                </h1>
                <p className="text-xl text-muted-foreground leading-relaxed max-w-2xl mx-auto mb-8">
                    Start for free, upgrade as you grow. No hidden fees for "extra seats" or "data ingestion."
                </p>
            </Section>

            {/* Pricing Grid */}
            <Section variant="muted" className="flex-grow">
                <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-5 gap-8 max-w-[1400px] mx-auto">

                    {/* Tier 1: Community */}
                    <Card variant="default">
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
                        <a href="https://github.com/Winnow-Triage" target="_blank" rel="noreferrer" className="w-full inline-flex items-center justify-center rounded-lg border border-slate-200 dark:border-slate-700 bg-transparent px-8 py-3 text-sm font-medium text-slate-900 dark:text-slate-50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                            <Users className="mr-2 h-4 w-4" /> View Source
                        </a>
                    </Card>

                    {/* Tier 2: Cloud Free */}
                    <Card variant="default" className="relative overflow-hidden">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-slate-900 dark:text-slate-50">Cloud Free</h3>
                            <p className="text-sm text-muted-foreground">To test the waters.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-slate-900 dark:text-slate-50">$0</span>
                            <span className="text-muted-foreground ml-2">/ month</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Fully Managed Hosting</PricingFeature>
                            <PricingFeature>Up to 1,000 reports / mo</PricingFeature>
                            <PricingFeature>1-Day Log Retention</PricingFeature>
                            <PricingFeature>Community Support</PricingFeature>
                        </ul>
                        <a href="http://localhost:5173/signup?tier=free" className="w-full inline-flex items-center justify-center rounded-lg border border-slate-200 dark:border-slate-700 bg-transparent px-8 py-3 text-sm font-medium text-slate-900 dark:text-slate-50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shadow-sm">
                            Try for Free
                        </a>
                    </Card>

                    {/* Tier 3: Cloud Starter */}
                    <Card variant="default" className="relative overflow-hidden">
                        <div className="mb-4">
                            <h3 className="text-xl font-bold text-slate-900 dark:text-slate-50">Cloud Starter</h3>
                            <p className="text-sm text-muted-foreground">For indie developers.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-4xl font-bold text-slate-900 dark:text-slate-50">$15</span>
                            <span className="text-muted-foreground ml-2">/ month</span>
                        </div>
                        <ul className="space-y-3 mb-8 flex-1">
                            <PricingFeature>Everything in Free</PricingFeature>
                            <PricingFeature>Unlimited Reports</PricingFeature>
                            <PricingFeature>7-Day Log Retention</PricingFeature>
                            <PricingFeature>Standard Email Support</PricingFeature>
                        </ul>
                        <a href="http://localhost:5173/signup?tier=starter" className="w-full inline-flex items-center justify-center rounded-lg bg-blue-600 px-8 py-3 text-sm font-medium text-white hover:bg-blue-700 transition-colors shadow-md">
                            Upgrade to Starter
                        </a>
                    </Card>

                    {/* Tier 4: Cloud Pro */}
                    <Card variant="primary" className="relative overflow-hidden hover:shadow-primary/20">
                        <div className="absolute top-0 right-0 bg-primary text-white text-xs font-bold px-3 py-1 rounded-bl-lg rounded-tr-lg uppercase">
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
                    </Card>

                    {/* Tier 5: Enterprise */}
                    <Card variant="dark">
                        <div className="mb-6">
                            <h3 className="text-xl font-bold text-white">Enterprise</h3>
                            <p className="text-sm text-slate-400">For enterprise compliance.</p>
                        </div>
                        <div className="mb-6">
                            <span className="text-3xl font-bold text-white tracking-tight">Custom</span>
                            <span className="text-slate-400 ml-2">Pricing</span>
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
                    </Card>

                </div>
            </Section>
        </div>
    );
}
