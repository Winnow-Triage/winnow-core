import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { Globe, Rocket, Shield, Heart, Coffee, ArrowRight } from 'lucide-react';

const jobs = [
    { title: "Senior Backend Engineer", team: "Core Infrastructure", location: "Remote / Texas", type: "Full-time" },
    { title: "Product Designer", team: "User Experience", location: "Remote", type: "Full-time" },
    { title: "DevOps Architect", team: "Reliability", location: "Remote / Texas", type: "Full-time" }
];

const benefits = [
    { title: "Remote-First", icon: Globe, description: "Work from anywhere in the world. We believe in high-agency, distributed teams." },
    { title: "Equity & Growth", icon: Rocket, description: "Own a piece of the company you're building. Every full-time hire gets equity." },
    { title: "Security Obsessed", icon: Shield, description: "We prioritize privacy and security in everything we build—and how we work." },
    { title: "Health & Wellness", icon: Heart, description: "Comprehensive healthcare for you and your family, plus mental health support." },
    { title: "Developer Tools", icon: Coffee, description: "Flexible budget for your ideal workspace and the tools you need to do your best work." }
];

export function Careers() {
    return (
        <div className="flex flex-col min-h-screen">
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                    Build the future of <br />
                    <GradientText>observability.</GradientText>
                </h1>
                <p className="text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                    We're a small, high-impact team dedicated to fixing the noise of modern software. Join us in building the autonomous triage layer.
                </p>
            </Section>

            <Section padding="large">
                <div className="max-w-3xl mx-auto text-center mb-16">
                    <h2 className="text-3xl font-bold mb-6">Why Winnow?</h2>
                    <p className="text-lg text-muted-foreground leading-relaxed">
                        We don't just build software. We build systems that solve real problems for developers. We value technical excellence, radical transparency, and a devotion to the craft.
                    </p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {benefits.map((benefit, i) => (
                        <div key={i} className="text-center p-6 grayscale hover:grayscale-0 transition-all">
                            <div className="h-16 w-16 rounded-full bg-slate-50 dark:bg-slate-900 border flex items-center justify-center mx-auto mb-6 text-primary">
                                <benefit.icon className="h-8 w-8" />
                            </div>
                            <h3 className="text-xl font-bold mb-3">{benefit.title}</h3>
                            <p className="text-muted-foreground text-sm leading-relaxed">
                                {benefit.description}
                            </p>
                        </div>
                    ))}
                </div>
            </Section>

            <Section variant="muted" border="top" padding="large">
                <div className="max-w-4xl mx-auto">
                    <div className="flex items-center justify-between mb-12">
                        <div>
                            <h2 className="text-3xl font-bold mb-2">Open Roles</h2>
                            <p className="text-muted-foreground">Find your place in our growing team.</p>
                        </div>
                        <div className="hidden md:block">
                            <span className="text-sm font-semibold uppercase tracking-widest text-primary bg-primary/10 px-4 py-2 rounded-full">3 Openings</span>
                        </div>
                    </div>
                    <div className="space-y-4">
                        {jobs.map((job, i) => (
                            <Card key={i} className="p-6 md:p-8 hover:border-primary/50 cursor-pointer group transition-all">
                                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                                    <div>
                                        <h3 className="text-xl font-bold mb-1 group-hover:text-primary transition-colors">{job.title}</h3>
                                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                                            <span>{job.team}</span>
                                            <span className="h-1 w-1 bg-slate-300 dark:bg-slate-700 rounded-full" />
                                            <span>{job.location}</span>
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-4">
                                        <span className="text-xs font-bold uppercase tracking-widest bg-slate-100 dark:bg-slate-800 px-3 py-1 rounded">{job.type}</span>
                                        <ArrowRight className="h-5 w-5 text-primary opacity-0 group-hover:opacity-100 group-hover:translate-x-1 transition-all" />
                                    </div>
                                </div>
                            </Card>
                        ))}
                    </div>
                </div>
            </Section>

            <Section padding="large" containerClassName="text-center">
                <div className="bg-slate-950 rounded-3xl p-12 md:p-24 text-white relative overflow-hidden">
                    <div className="absolute top-0 right-0 p-8 opacity-10">
                        <Rocket className="h-64 w-64" />
                    </div>
                    <div className="relative z-10">
                        <h2 className="text-3xl md:text-5xl font-bold mb-8">Don't see your role?</h2>
                        <p className="text-xl text-slate-400 max-w-2xl mx-auto mb-12">
                            We're always looking for exceptional talent. If you're passionate about observability and AI, reach out and let's talk.
                        </p>
                        <button className="h-12 items-center justify-center rounded-full bg-white px-10 text-sm font-bold text-slate-950 shadow transition-colors hover:bg-slate-200">
                            General Application
                        </button>
                    </div>
                </div>
            </Section>
        </div>
    );
}
