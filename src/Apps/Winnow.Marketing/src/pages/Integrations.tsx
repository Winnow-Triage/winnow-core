import {
    Zap,
    ArrowRight
} from 'lucide-react';
import {
    SiDiscord,
    SiGithub,
    SiJira,
    SiUnity,
    SiJavascript,
    SiGodotengine
} from 'react-icons/si';
import {
    FaTrello,
    FaSlack,
    FaMicrosoft
} from 'react-icons/fa';
import { Link } from 'react-router-dom';
import { CTA } from '../components/CTA';

const categories = [
    {
        name: "Communication",
        description: "Get real-time alerts where your team already works.",
        integrations: [
            { name: "Slack", icon: FaSlack, description: "Instant notifications and cluster summaries in any channel.", status: "Coming Soon" },
            { name: "Discord", icon: SiDiscord, description: "Rich webhooks for gaming and community-driven projects.", status: "Coming Soon" },
            { name: "MS Teams", icon: FaMicrosoft, description: "Enterprise-grade alerts for structured organizations.", status: "Coming Soon" }
        ]
    },
    {
        name: "Issue Tracking",
        description: "Seamlessly turn clusters into actionable tickets.",
        integrations: [
            { name: "Jira", icon: SiJira, description: "Create and link Jira tickets directly from the Winnow UI.", status: "Available" },
            { name: "GitHub Issues", icon: SiGithub, description: "Sync crash reports with your repository's issues.", status: "Available" },
            { name: "Trello", icon: FaTrello, description: "Create and link Trello cards directly from the Winnow UI.", status: "Available" }
        ]
    },
    {
        name: "Developer SDKs",
        description: "First-class support for your favorite platforms.",
        integrations: [
            { name: "Javascript", icon: SiJavascript, description: "Javascript SDK for web, desktop, and mobile applications.", status: "v0.1.0" },
            { name: "Unity", icon: SiUnity, description: "Automatic game crash capture and performance monitoring.", status: "v0.1.0" },
            { name: "Godot", icon: SiGodotengine, description: "Lightweight C# AutoLoad for Godot 4 (.NET) crash reporting", status: "v0.1.0" }
        ]
    }
];

export function Integrations() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Hero Section */}
            <section className="relative py-24 md:py-32 overflow-hidden bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-white transition-colors duration-300 border-b">
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full h-full max-w-7xl pointer-events-none">
                    <div className="absolute top-[-10%] right-[-10%] w-[40%] h-[40%] bg-blue-500/10 blur-[120px] rounded-full" />
                    <div className="absolute bottom-[-10%] left-[-10%] w-[40%] h-[40%] bg-purple-500/10 blur-[120px] rounded-full" />
                </div>
                <div className="absolute inset-0 bg-grid-slate-950/[0.02] dark:bg-grid-white/[0.02] pointer-events-none" />
                <div className="container mx-auto px-4 md:px-6 relative z-10 text-center">
                    <div className="max-w-3xl mx-auto">
                        <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                            Connect your <br />
                            <span className="text-brand-gradient italic">entire stack.</span>
                        </h1>
                        <p className="text-xl text-muted-foreground leading-relaxed max-w-2xl mx-auto mb-8">
                            Winnow integrates deeply with the tools you already love. No more context switching—just streamlined triage.
                        </p>
                    </div>
                </div>
            </section>

            {/* Categories Grid */}
            <section className="py-20 bg-slate-50 dark:bg-slate-900/50 flex-grow">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="space-y-24">
                        {categories.map((category, i) => (
                            <div key={i}>
                                <div className="mb-12">
                                    <h2 className="text-3xl font-bold mb-2">{category.name}</h2>
                                    <p className="text-muted-foreground">{category.description}</p>
                                </div>
                                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                                    {category.integrations.map((item, j) => (
                                        <div key={j} className="group relative bg-white dark:bg-slate-950 p-8 rounded-3xl border shadow-sm hover:shadow-xl hover:-translate-y-1 transition-all duration-300">
                                            <div className="flex items-start justify-between mb-6">
                                                <div className="h-12 w-12 rounded-2xl bg-slate-50 dark:bg-slate-900 border flex items-center justify-center group-hover:bg-primary group-hover:text-primary-foreground transition-colors duration-300">
                                                    <item.icon className="h-6 w-6" />
                                                </div>
                                                <span className="text-[10px] font-bold uppercase tracking-widest px-2 py-1 bg-slate-100 dark:bg-slate-800 rounded">
                                                    {item.status}
                                                </span>
                                            </div>
                                            <h3 className="text-xl font-bold mb-3">{item.name}</h3>
                                            <p className="text-muted-foreground text-sm leading-relaxed mb-6">
                                                {item.description}
                                            </p>
                                            <div className="flex items-center text-primary text-sm font-semibold opacity-0 group-hover:opacity-100 transition-opacity">
                                                Install Guide <ArrowRight className="h-4 w-4 ml-1" />
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* Footer Build CTA */}
            <section className="py-20 border-t bg-white dark:bg-slate-950">
                <div className="container mx-auto px-4 md:px-6 text-center">
                    <h2 className="text-3xl font-bold mb-4">Don't see your tool?</h2>
                    <p className="text-muted-foreground mb-8 max-w-md mx-auto">We're constantly adding new integrations. Build your own using our public API.</p>
                    <div className="flex flex-col sm:flex-row gap-4 justify-center mb-12">
                        <Link to="/docs" className="inline-flex h-11 items-center justify-center rounded-full border-2 border-primary/20 bg-background px-8 py-2 text-sm font-bold shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground">
                            Read API Docs
                        </Link>
                        <Link to="/contact" className="inline-flex h-11 items-center justify-center rounded-full border-2 border-primary/20 bg-background px-8 py-2 text-sm font-bold shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground">
                            Request Integration
                        </Link>
                    </div>
                </div>
            </section>

            <CTA />
        </div>
    );
}
