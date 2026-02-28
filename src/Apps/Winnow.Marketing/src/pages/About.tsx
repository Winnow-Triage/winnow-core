import { Target, Users, Coffee, Compass, Zap, Sparkles, MapPin } from 'lucide-react';
import { CTA } from '../components/CTA';
import { SystemActivityVisual } from '../components/SystemActivityVisual';

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
            <section className="relative py-24 md:py-32 overflow-hidden bg-slate-950 text-white">
                <div className="absolute inset-0 opacity-20 bg-[url('https://www.transparenttextures.com/patterns/carbon-fibre.png')]" />
                <div className="absolute top-0 right-0 w-1/2 h-full bg-gradient-to-l from-blue-600/10 to-transparent pointer-none" />
                <div className="container mx-auto px-4 md:px-6 relative z-10 text-center">
                    <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-blue-500/10 border border-blue-500/20 text-blue-400 text-xs font-bold mb-8">
                        <Sparkles className="h-3 w-3" /> OUR STORY & VISION
                    </div>
                    <h1 className="text-4xl md:text-6xl font-bold mb-8 tracking-tighter">
                        We're on a mission to <br />
                        <span className="bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent italic">
                            eliminate alert fatigue.
                        </span>
                    </h1>
                    <p className="text-xl text-slate-400 max-w-3xl mx-auto leading-relaxed mb-10">
                        Winnow was born out of the frustration of managing thousands of duplicate crash reports at scale. We believed there was a better way to triage, and we built it—leveraging AI not just as a buzzword, but as the foundation of semantic observability.
                    </p>
                    <div className="flex items-center justify-center gap-2 text-blue-400/80 font-medium italic">
                        <MapPin className="h-4 w-4" />
                        <span>Proudly built in Fort Worth, Texas by a developer who got tired of closing duplicate tickets.</span>
                    </div>
                </div>
            </section>

            {/* Content Section */}
            <section className="py-20 md:py-32">
                <div className="container mx-auto px-4 md:px-6">
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
                        <div className="bg-slate-50 dark:bg-slate-900/50 p-8 md:p-12 rounded-3xl border">
                            <h2 className="text-3xl font-bold mb-8">Our Core Values</h2>
                            <div className="space-y-12">
                                {values.map((value, i) => (
                                    <div key={i} className="flex gap-6">
                                        <div className="mt-1 h-12 w-12 shrink-0 rounded-2xl bg-white dark:bg-slate-950 border flex items-center justify-center shadow-sm">
                                            <value.icon className={`h-6 w-6 ${value.color}`} />
                                        </div>
                                        <div>
                                            <h3 className="font-bold text-xl mb-2">{value.title}</h3>
                                            <p className="text-muted-foreground leading-relaxed">{value.description}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* Vision Section */}
            <section className="py-20 md:py-32 bg-slate-50 dark:bg-slate-900/50 border-y">
                <div className="container mx-auto px-4 md:px-6">
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
                </div>
            </section>

            {/* Philosophy Section */}
            <section className="py-20 md:py-32">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="max-w-3xl mx-auto text-center mb-16">
                        <h2 className="text-3xl font-bold mb-6">Our Technical Philosophy</h2>
                        <p className="text-xl text-muted-foreground leading-relaxed">
                            We believe in high-leverage tools. That's why we built Winnow on top of vector-based similarity engines, not rigid string-matching databases.
                        </p>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                        <div className="p-8 rounded-3xl border bg-card hover:shadow-lg transition-shadow">
                            <Zap className="h-8 w-8 text-amber-500 mb-4" />
                            <h3 className="font-bold text-xl mb-4">Vector-First</h3>
                            <p className="text-muted-foreground text-sm">Every report is converted into a high-dimensional vector. This allows us to find "near-matches" that traditional software would miss completely.</p>
                        </div>
                        <div className="p-8 rounded-3xl border bg-card hover:shadow-lg transition-shadow">
                            <Users className="h-8 w-8 text-blue-500 mb-4" />
                            <h3 className="font-bold text-xl mb-4">Collaborative Context</h3>
                            <p className="text-muted-foreground text-sm">Data is useless without team context. Every feature we build is designed to make it easier for teams to share knowledge across projects.</p>
                        </div>
                        <div className="p-8 rounded-3xl border bg-card hover:shadow-lg transition-shadow">
                            <Coffee className="h-8 w-8 text-emerald-500 mb-4" />
                            <h3 className="font-bold text-xl mb-4">Developer Delight</h3>
                            <p className="text-muted-foreground text-sm">Low latency, zero bloat, and APIs that feel like they were written by friends. We prioritize the developer experience above all else.</p>
                        </div>
                    </div>
                </div>
            </section>

            {/* CTA Section */}
            <CTA />
        </div>
    );
}
