import { Target, Users, Coffee, Compass, Zap, MapPin, Github, ShieldCheck } from 'lucide-react';
import { CTA } from '../components/CTA';
import { SystemActivityVisual } from '../components/SystemActivityVisual';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';

const values = [
    {
        title: "Developer First",
        description: "We build tools we actually want to use. That means low latency, great APIs, and frictionless integrations.",
        icon: Coffee,
        color: "text-amber-500"
    },
    {
        title: "Zero-Noise Mission",
        description: "Alert fatigue kills productivity. We are committed to using AI to filter out the noise so you can focus on what matters.",
        icon: Target,
        color: "text-red-500"
    },
    {
        title: "Transparent & Secure",
        description: "Your data is handled with the highest security standards. We're open about how our AI works and how your data is used.",
        icon: Users,
        color: "text-blue-500"
    }
];

export function About() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Mission Hero */}
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6 mt-4">
                    We're on a mission to <br />
                    <GradientText>eliminate alert fatigue.</GradientText>
                </h1>
                <p className="text-xl text-slate-600 dark:text-slate-400 max-w-3xl mx-auto leading-relaxed mb-10 transition-colors duration-300">
                    Winnow was born out of the frustration of managing thousands of duplicate crash reports at scale. We believed there was a better way to triage, and we built it—leveraging AI not just as a buzzword, but as the foundation of semantic observability.
                </p>
                <div className="flex items-center justify-center gap-2 text-blue-400/80 font-medium italic">
                    <MapPin className="h-4 w-4" />
                    <span>Proudly built in Fort Worth, Texas by a developer who got tired of closing duplicate tickets.</span>
                </div>
            </Section>

            {/* Content Section */}
            <Section padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-start">
                    <div className="space-y-6 text-lg text-muted-foreground leading-relaxed">
                        <h2 className="text-3xl font-bold text-foreground">The Problem We're Solving</h2>
                        <p>
                            Modern applications generate millions of data points every day. When a bug hits production, it's rarely just one report—it's hundreds or thousands of identical signals flooding into your inbox.
                        </p>
                        <p>
                            Traditional tools treat every report as a new event, forcing developers to manually identify patterns and deduplicate tickets. This "noise" obscures the true impact of issues and causes burnout.
                        </p>
                        <p className="font-medium text-foreground italic border-l-4 border-primary pl-6 py-2">
                            Winnow uses AI-driven vector similarity to understand your reports semantically. It's like having a senior engineer constantly triaging your inbox.
                        </p>
                    </div>
                    <Card variant="default" isHoverable={false} className="bg-slate-50 dark:bg-slate-900/50 p-8 md:p-12">
                        <h2 className="text-3xl font-bold mb-10">Our Core Values</h2>
                        <div className="space-y-12">
                            {values.map((value, i) => (
                                <div key={i} className="flex gap-6 group transition-all duration-300">
                                    <div className="mt-1 h-12 w-12 shrink-0 rounded-2xl bg-white dark:bg-slate-950 border flex items-center justify-center shadow-sm group-hover:scale-110 group-hover:shadow-md transition-all duration-300">
                                        <value.icon className={`h-6 w-6 ${value.color}`} />
                                    </div>
                                    <div>
                                        <h3 className="font-bold text-xl mb-2">{value.title}</h3>
                                        <p className="text-muted-foreground leading-relaxed">{value.description}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </Card>
                </div>
            </Section>

            {/* Vision Section */}
            <Section variant="muted" border="both" padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
                    <div className="order-2 lg:order-1">
                        <SystemActivityVisual />
                    </div>
                    <div className="order-1 lg:order-2 space-y-6">
                        <h2 className="text-3xl font-bold flex items-center gap-3">
                            <Compass className="h-8 w-8 text-blue-500" /> The Future of Triage
                        </h2>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            We aren't just building a faster crash reporter. We're building an autonomous observability layer that predicts and prevents downtime before it impacts your users.
                        </p>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            Imagine a world where your infrastructure heals itself based on real-time failure patterns, and "critical alerts" are a relic of the past. That's the world Winnow is creating.
                        </p>
                    </div>
                </div>
            </Section>

            {/* Philosophy Section */}
            <Section padding="large">
                <div className="max-w-3xl mx-auto text-center mb-16">
                    <h2 className="text-3xl font-bold mb-6">Our Technical Philosophy</h2>
                    <p className="text-xl text-muted-foreground leading-relaxed">
                        We believe in high-leverage tools. That's why we built Winnow on top of vector-based similarity engines, not rigid string-matching databases.
                    </p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                    <Card>
                        <Zap className="h-8 w-8 text-amber-500 mb-6" />
                        <h3 className="font-bold text-xl mb-4">Vector-First</h3>
                        <p className="text-muted-foreground text-sm leading-relaxed">Every report is converted into a high-dimensional vector. This allows us to find "near-matches" that traditional software would miss completely.</p>
                    </Card>
                    <Card>
                        <Users className="h-8 w-8 text-blue-500 mb-6" />
                        <h3 className="font-bold text-xl mb-4">Collaborative Context</h3>
                        <p className="text-muted-foreground text-sm leading-relaxed">Data is useless without team context. Every feature we build is designed to make it easier for teams to share knowledge across projects.</p>
                    </Card>
                    <Card>
                        <Coffee className="h-8 w-8 text-emerald-500 mb-6" />
                        <h3 className="font-bold text-xl mb-4">Developer Delight</h3>
                        <p className="text-muted-foreground text-sm leading-relaxed">Low latency, zero bloat, and APIs that feel like they were written by friends. We prioritize the developer experience above all else.</p>
                    </Card>
                </div>
            </Section>

            {/* Developer Transparency Section */}
            <Section variant="muted" border="top" padding="large">
                <div className="max-w-3xl mx-auto text-center mb-16">
                    <h2 className="text-3xl font-bold mb-6">Developer Transparency</h2>
                    <p className="text-xl text-muted-foreground leading-relaxed">
                        Winnow is built by developers, for developers. We're committed to open-source and the highest security standards.
                    </p>
                </div>
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                    <Card isHoverable={false} className="p-8 md:p-12 bg-card/50 backdrop-blur-sm">
                        <div className="flex items-center gap-4 mb-6">
                            <div className="h-12 w-12 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
                                <Github className="h-6 w-6 text-blue-500" />
                            </div>
                            <h3 className="text-2xl font-bold">Open-Source Core</h3>
                        </div>
                        <p className="text-muted-foreground leading-relaxed mb-6">
                            We believe the best developer tools are open and extensible. Winnow's core is open-source, allowing you to audit the code, contribute features, or self-host for complete data sovereignty.
                        </p>
                        <a href="https://github.com/Winnow-Triage" target="_blank" rel="noreferrer" className="w-full inline-flex items-center justify-center rounded-lg border border-slate-200 dark:border-slate-700 bg-transparent px-8 py-3 text-sm font-medium text-slate-900 dark:text-slate-50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                            <Github className="h-4 w-4" /> View Source on GitHub
                        </a>
                        <div className="flex flex-wrap gap-3">
                            <span className="px-3 py-1 rounded-full bg-blue-500/5 border border-blue-500/10 text-[10px] font-bold uppercase tracking-wider text-blue-500/80">MIT Licensed</span>
                            <span className="px-3 py-1 rounded-full bg-blue-500/5 border border-blue-500/10 text-[10px] font-bold uppercase tracking-wider text-blue-500/80">Self-Hostable</span>
                            <span className="px-3 py-1 rounded-full bg-blue-500/5 border border-blue-500/10 text-[10px] font-bold uppercase tracking-wider text-blue-500/80">API-First</span>
                        </div>
                    </Card>

                    <Card isHoverable={false} className="p-8 md:p-12 bg-card/50 backdrop-blur-sm">
                        <div className="flex items-center gap-4 mb-6">
                            <div className="h-12 w-12 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                                <ShieldCheck className="h-6 w-6 text-emerald-500" />
                            </div>
                            <h3 className="text-2xl font-bold">Security Standards</h3>
                        </div>
                        <ul className="space-y-4 text-muted-foreground">
                            <li className="flex items-start gap-3">
                                <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong className="text-foreground font-semibold">Passwords:</strong> Hashed with BCrypt (work factor 11) for maximum brute-force resistance.</span>
                            </li>
                            <li className="flex items-start gap-3">
                                <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong className="text-foreground font-semibold">Integrations:</strong> AES-256-GCM authenticated encryption for all sensitive third-party keys and tokens.</span>
                            </li>
                            <li className="flex items-start gap-3">
                                <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong className="text-foreground font-semibold">API Keys:</strong> Argon2id or SHA-384 hashing for all project keys; we never store secrets in plaintext.</span>
                            </li>
                            <li className="flex items-start gap-3">
                                <div className="mt-1.5 h-1.5 w-1.5 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong className="text-foreground font-semibold">Data Privacy:</strong> Multi-tenant isolation ensuring your data is never accessible by others.</span>
                            </li>
                        </ul>
                    </Card>
                </div>
            </Section>

            {/* CTA Section */}
            <CTA />
        </div>
    );
}
