import { Target, Users, Coffee, Compass, MapPin, Github, ShieldCheck, Shield, Rocket, MessageSquare, Mail } from 'lucide-react';
import { CTA } from '../components/CTA';
import { SystemActivityVisual } from '../components/SystemActivityVisual';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { SEOMeta } from '../components/SEOMeta';
import { FaDiscord } from 'react-icons/fa';

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
            <SEOMeta
                title="About"
                description="Built for builders. Learn about Winnow's mission to eliminate alert fatigue through independent, vector-first engineering."
            />
            {/* The Technical Philosophy Hero */}
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6 mt-4 leading-[1.1]">
                    Solving the "Noise" <br />
                    <GradientText>with Semantic Observability.</GradientText>
                </h1>
                <p className="text-xl text-slate-600 dark:text-slate-400 max-w-3xl mx-auto leading-relaxed mb-10 transition-colors duration-300">
                    Most observability tools treat every log as a unique event. Winnow is different. We believe in high-leverage tools built on vector-based similarity engines, not rigid string-matching.
                </p>
                <div className="flex items-center justify-center gap-2 text-primary font-medium italic">
                    <Target className="h-4 w-4" />
                    <span>Eliminating alert fatigue through deep technical triage.</span>
                </div>
            </Section>

            {/* Technical Philosophy Details */}
            <Section padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-start">
                    <div className="space-y-6 text-lg text-muted-foreground leading-relaxed">
                        <h2 className="text-3xl font-bold text-foreground">The Tech Behind the <GradientText>Triage</GradientText></h2>
                        <p>
                            Modern applications generate millions of signals. When a bug hits production, it's rarely just one report—it's thousands of identical signals flooding into your inbox.
                        </p>
                        <p>
                            Traditional tools force developers to manually deduplicate tickets. Winnow uses **vector-first similarity** to understand your reports semantically. It's like having a senior engineer constantly triaging your inbox.
                        </p>
                        <p className="font-medium text-foreground italic border-l-4 border-primary pl-6 py-2">
                            Every report is converted into a high-dimensional vector, allowing us to find "near-matches" that traditional software misses.
                        </p>
                    </div>
                    <Card variant="default" isHoverable={false} className="bg-slate-50 dark:bg-slate-900/50 p-8 md:p-12">
                        <h2 className="text-3xl font-bold mb-10">Engineering Values</h2>
                        <div className="space-y-12">
                            {values.map((value, i) => (
                                <div key={i} className="flex gap-6 group transition-all duration-300">
                                    <div className="mt-1 h-12 w-12 shrink-0 rounded-2xl bg-white dark:bg-slate-950 border flex items-center justify-center shadow-sm group-hover:scale-110 group-hover:shadow-md transition-all duration-300">
                                        <value.icon className={`h-6 w-6 ${value.color}`} />
                                    </div>
                                    <div>
                                        <h3 className="font-bold text-xl mb-2">{value.title}</h3>
                                        <p className="text-muted-foreground leading-relaxed text-sm">{value.description}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </Card>
                </div>
            </Section>

            {/* The Human Element: Solo-Founder Section */}
            <Section variant="muted" border="both" padding="large">
                <div className="max-w-4xl mx-auto">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-16 items-center">
                        <div className="order-2 md:order-1 grid grid-cols-2 gap-6">
                            <div className="p-8 rounded-3xl bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800 text-center">
                                <Rocket className="h-8 w-8 text-primary mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">100%</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Self-Funded</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800 text-center">
                                <Shield className="h-8 w-8 text-amber-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Independent</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">No VC Pressure</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800 text-center">
                                <Coffee className="h-8 w-8 text-indigo-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Solo</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Founded</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-white dark:bg-slate-950 border border-slate-100 dark:border-slate-800 text-center">
                                <MessageSquare className="h-8 w-8 text-emerald-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Direct</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Support</div>
                            </div>
                        </div>
                        <div className="order-1 md:order-2 space-y-6">
                            <h2 className="text-3xl md:text-4xl font-bold">
                                A personal mission, <br />
                                <GradientText>not a corporate plan.</GradientText>
                            </h2>
                            <div className="space-y-4 text-lg text-muted-foreground leading-relaxed">
                                <p>
                                    Winnow isn't a venture-backed growth machine. It's a bootstrapped engineering lab built by a solo developer in Fort Worth, Texas.
                                </p>
                                <p>
                                    I started Winnow to solve my own burnout. I wanted a tool that actually understood my logs, not just one that stored them. Because we have no investor pressure, we answer only to our users.
                                </p>
                                <div className="flex items-center gap-2 text-primary font-medium text-sm pt-4 italic">
                                    <MapPin className="h-4 w-4" />
                                    <span>Built with care in Fort Worth, TX.</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </Section>

            {/* Vision & Consistency */}
            <Section padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
                    <div className="space-y-6">
                        <h2 className="text-3xl font-bold flex items-center gap-3">
                            <Compass className="h-8 w-8 text-blue-500" /> The Future of <GradientText>Triage</GradientText>
                        </h2>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            We aren't just building a faster crash reporter. We're building an autonomous observability layer that predicts and prevents downtime.
                        </p>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            Imagine infrastructure that heals itself based on real-time patterns. That's the world Winnow is creating.
                        </p>
                    </div>
                    <SystemActivityVisual />
                </div>
            </Section>

            {/* Security Transparency */}
            <Section variant="muted" border="top" padding="large">
                <div className="max-w-3xl mx-auto text-center mb-16">
                    <h2 className="text-3xl font-bold mb-6">Security and <GradientText>Transparency</GradientText></h2>
                    <p className="text-xl text-muted-foreground leading-relaxed">
                        As a solo-founder tool, trust is everything. We maintain the highest security standards so you can focus on building.
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
                            Winnow's core is open-source. Audit the code, contribute features, or self-host for complete data sovereignty.
                        </p>
                        <a href="https://github.com/Winnow-Triage" target="_blank" rel="noreferrer" className="w-full inline-flex items-center justify-center rounded-lg border border-slate-200 dark:border-slate-700 bg-transparent px-8 py-3 text-sm font-medium hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
                            <Github className="h-4 w-4 mr-2" /> View on GitHub
                        </a>
                    </Card>

                    <Card isHoverable={false} className="p-8 md:p-12 bg-card/50 backdrop-blur-sm">
                        <div className="flex items-center gap-4 mb-6">
                            <div className="h-12 w-12 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center">
                                <ShieldCheck className="h-6 w-6 text-emerald-500" />
                            </div>
                            <h3 className="text-2xl font-bold">Security Standards</h3>
                        </div>
                        <ul className="space-y-3 text-xs text-muted-foreground">
                            <li className="flex items-start gap-2">
                                <div className="mt-1 h-1 w-1 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong>Passwords:</strong> Hashed with BCrypt (work factor 11).</span>
                            </li>
                            <li className="flex items-start gap-2">
                                <div className="mt-1 h-1 w-1 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong>Integrations:</strong> AES-256-GCM authenticated encryption for sensitive keys.</span>
                            </li>
                            <li className="flex items-start gap-2">
                                <div className="mt-1 h-1 w-1 rounded-full bg-emerald-500 shrink-0" />
                                <span><strong>API Keys:</strong> Argon2id or SHA-384 hashing; no plaintext storage.</span>
                            </li>
                        </ul>
                    </Card>
                </div>
            </Section>

            {/* Connection Section */}
            <Section padding="large" containerClassName="text-center">
                <div className="max-w-3xl mx-auto">
                    <h2 className="text-3xl font-bold mb-8">Follow the <GradientText>Journey</GradientText></h2>
                    <p className="text-lg text-muted-foreground mb-12">
                        Whether you're a user or just interested in how we're building an independent observability tool, stay connected.
                    </p>
                    <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
                        <a href="https://discord.gg/winnow" target="_blank" rel="noreferrer" className="inline-flex items-center gap-2 px-8 py-4 bg-[#5865F2] text-white rounded-full font-bold transition-transform hover:scale-105">
                            <FaDiscord className="h-5 w-5 fill-white" /> Join Discord
                        </a>
                        <a href="mailto:james@winnowtriage.com" className="inline-flex items-center gap-2 px-8 py-4 bg-white border border-slate-200 text-slate-900 rounded-full font-bold transition-transform hover:scale-105">
                            <Mail className="h-5 w-5" /> Direct Email
                        </a>
                    </div>
                </div>
            </Section>

            <CTA />
        </div>
    );
}
