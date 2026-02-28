import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { CTA } from '../components/CTA';
import {
    GitMerge,
    Microscope,
    Zap,
    Shield,
    BarChart3,
    Cpu,
    Slack,
    Github,
    Layout,
    ArrowRight,
    Users2,
    Key,
    Eye,
    Video,
    MessageSquare,
    Lock,
    CheckCircle2,
    AlertCircle
} from 'lucide-react';

function ScanningSimulator() {
    const items = [
        { type: 'Image', icon: Eye, label: 'IMG_4821.jpg' },
        { type: 'Video', icon: Video, label: 'USER_UPLOAD.mp4' },
        { type: 'Text', icon: MessageSquare, label: 'comment_body.txt' }
    ];

    const [currentItemIdx, setCurrentItemIdx] = useState(0);
    const [status, setStatus] = useState<'scanning' | 'passed' | 'failed'>('scanning');
    const [progress, setProgress] = useState(0);

    useEffect(() => {
        let timer: any;
        if (status === 'scanning') {
            timer = setInterval(() => {
                setProgress(prev => {
                    if (prev >= 100) {
                        clearInterval(timer);
                        const result = Math.random() > 0.3 ? 'passed' : 'failed';
                        setStatus(result);
                        return 100;
                    }
                    return prev + 2.5;
                });
            }, 40);
        } else {
            timer = setTimeout(() => {
                setStatus('scanning');
                setProgress(0);
                setCurrentItemIdx(prev => (prev + 1) % items.length);
            }, 2000);
        }
        return () => {
            clearInterval(timer);
            clearTimeout(timer);
        };
    }, [status, items.length]);

    const item = items[currentItemIdx];

    return (
        <div className="relative bg-white dark:bg-slate-950 rounded-2xl border aspect-video shadow-2xl overflow-hidden flex flex-col items-center justify-center p-8">
            <div className={`absolute inset-0 bg-slate-100 dark:bg-slate-900 transition-opacity duration-1000 ${status === 'scanning' ? 'opacity-50' : 'opacity-20'}`} />

            {/* Scanning Grid Background */}
            <div className="absolute inset-0 opacity-[0.03] dark:opacity-[0.05] pointer-events-none">
                <div className="h-full w-full bg-[linear-gradient(to_right,#80808012_1px,transparent_1px),linear-gradient(to_bottom,#80808012_1px,transparent_1px)] bg-[size:24px_24px]"></div>
            </div>

            <div className="relative z-10 w-full max-w-xs space-y-6">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <div className="h-8 w-8 rounded-lg bg-primary/10 flex items-center justify-center">
                            <item.icon className="h-4 w-4 text-primary" />
                        </div>
                        <span className="text-sm font-medium font-mono text-slate-600 dark:text-slate-400">{item.label}</span>
                    </div>
                    {status !== 'scanning' && (
                        <div className={`flex items-center gap-1.5 px-2 py-1 rounded-md text-[10px] font-bold uppercase tracking-wider ${status === 'passed' ? 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600' : 'bg-red-100 dark:bg-red-900/30 text-red-600'
                            }`}>
                            {status === 'passed' ? <CheckCircle2 className="h-3 w-3" /> : <AlertCircle className="h-3 w-3" />}
                            {status}
                        </div>
                    )}
                </div>

                <div className="relative h-1.5 w-full bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                    <div
                        className={`absolute top-0 left-0 h-full transition-all duration-75 ${status === 'failed' ? 'bg-red-500' : status === 'passed' ? 'bg-emerald-500' : 'bg-primary'
                            }`}
                        style={{ width: `${progress}%` }}
                    />
                </div>

                <div className="flex justify-between items-center text-[10px] font-mono uppercase tracking-widest text-slate-500 bg-slate-100/50 dark:bg-slate-900/50 px-2 py-1 rounded">
                    <span>{status === 'scanning' ? 'Analyzing patterns...' : 'Processing complete'}</span>
                    <span>{Math.round(progress)}%</span>
                </div>
            </div>

            {/* Scanning Line Overlay */}
            {status === 'scanning' && (
                <div
                    className="absolute inset-x-0 h-1 bg-gradient-to-r from-transparent via-primary to-transparent opacity-50 shadow-[0_0_15px_rgba(var(--primary),0.8)] z-20 pointer-events-none"
                    style={{ top: `${progress}%` }}
                />
            )}
        </div>
    );
}

const coreFeatures = [
    {
        title: "AI-Powered Deduplication",
        description: "Winnow uses advanced vector embeddings to understand the semantic meaning of your crash reports. Instead of exact string matching, we group reports that are conceptually identical, saving you from alert fatigue.",
        icon: GitMerge,
        color: "text-purple-500",
        bg: "bg-purple-100 dark:bg-purple-900/20"
    },
    {
        title: "Forensic Ingestion",
        description: "Every report includes a full snapshot of the application state: console logs, network requests, device metadata, and a high-resolution screenshot. No more asking users 'what happened?'.",
        icon: Microscope,
        color: "text-primary",
        bg: "bg-primary/10"
    },
    {
        title: "Real-time Priority Scoring",
        description: "Our AI analyzes the frequency, spread, and severity of clusters to assign an impact score. Focus on the bugs affecting 80% of your users, not the edge cases.",
        icon: BarChart3,
        color: "text-amber-500",
        bg: "bg-amber-100 dark:bg-amber-900/20"
    }
];

const technicalSpecs = [
    {
        title: "On-device Pre-filtration",
        description: "Our SDKs are designed to be lightweight. They handle noise filtration and rate limiting locally before reports ever reach your infrastructure.",
        icon: Shield
    },
    {
        title: "Vector-first Database",
        description: "We built Winnow on high-performance vector stores, allowing for sub-millisecond similarity searches across millions of historical reports.",
        icon: Cpu
    },
    {
        title: "Modern Export Flow",
        description: "Directly push triaged clusters to Jira, GitHub, or Linear. Tickets are automatically updated as new reports join the cluster.",
        icon: Layout
    },
    {
        title: "Edge Delivery",
        description: "Ingestion endpoints are distributed globally on a high-availability CDN, ensuring low latency for your users worldwide.",
        icon: Zap
    }
];

export function Features() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Hero Section */}
            <section className="relative pt-20 pb-16 md:pt-32 md:pb-24 overflow-hidden border-b">
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full h-full max-w-7xl pointer-events-none">
                    <div className="absolute top-1/4 left-1/4 w-[2000px] h-[2000px] bg-blue-600/5 blur-[1000px] rounded-full animate-drift pointer-events-none"></div>
                    <div className="absolute bottom-1/4 right-1/4 w-[2000px] h-[2000px] bg-purple-600/5 blur-[1000px] rounded-full animate-drift [animation-delay:-7s] pointer-events-none"></div>
                </div>
                <div className="absolute inset-0 bg-grid-slate-950/[0.02] dark:bg-grid-white/[0.02] pointer-events-none" />

                <div className="container mx-auto px-4 md:px-6 relative z-10 text-center">
                    <div className="max-w-3xl mx-auto">
                        <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                            Next-generation triage, <br />
                            <span className="text-brand-gradient italic">powered by vectors.</span>
                        </h1>
                        <p className="text-xl text-muted-foreground leading-relaxed mb-8">
                            Winnow isn't just another bug tracker. It's an intelligent triage engine that understands your application's failures as deeply as you do.
                        </p>
                    </div>
                </div>
            </section>

            {/* Core Features Grid */}
            <section className="py-20 md:py-32 bg-slate-50 dark:bg-slate-900/50">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center mb-32">
                        <div>
                            <h2 className="text-3xl md:text-4xl font-bold mb-6">Stop chasing shadows. <br />Start fixing bugs.</h2>
                            <div className="space-y-8">
                                {coreFeatures.map((feature, i) => (
                                    <div key={i} className="flex gap-4">
                                        <div className={`mt-1 h-10 w-10 shrink-0 rounded-lg ${feature.bg} flex items-center justify-center`}>
                                            <feature.icon className={`h-6 w-6 ${feature.color}`} />
                                        </div>
                                        <div>
                                            <h3 className="font-bold text-xl mb-2">{feature.title}</h3>
                                            <p className="text-muted-foreground leading-relaxed">{feature.description}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                        <div className="relative">
                            <div className="aspect-square bg-gradient-to-br from-primary/20 to-purple-600/20 rounded-3xl border border-white/10 shadow-2xl overflow-hidden flex items-center justify-center p-8">
                                <div className="absolute inset-0 bg-grid-slate-900/[0.04] dark:bg-grid-white/[0.02]" />
                                <div className="relative bg-white dark:bg-slate-950 rounded-2xl border p-6 shadow-xl w-full max-w-md transform transition-all hover:scale-105">
                                    <div className="flex items-center gap-3 mb-4">
                                        <div className="h-3 w-3 rounded-full bg-red-500" />
                                        <div className="h-3 w-3 rounded-full bg-amber-500" />
                                        <div className="h-3 w-3 rounded-full bg-green-500" />
                                    </div>
                                    <div className="space-y-4">
                                        <div className="h-4 w-3/4 bg-slate-100 dark:bg-slate-800 rounded animate-pulse" />
                                        <div className="h-20 w-full bg-slate-50 dark:bg-slate-900 rounded border border-dashed border-slate-200 dark:border-slate-800 flex items-center justify-center">
                                            <span className="text-xs text-slate-400">AI Cluster: "NullReference in Auth Flow"</span>
                                        </div>
                                        <div className="flex gap-2">
                                            <div className="h-6 w-16 bg-primary/10 rounded flex items-center justify-center">
                                                <span className="text-[10px] text-primary">841 reports</span>
                                            </div>
                                            <div className="h-6 w-20 bg-purple-100 dark:bg-purple-900/40 rounded flex items-center justify-center">
                                                <span className="text-[10px] text-purple-600">High Impact</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* Integrations Preview */}
            <section className="py-20 bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-white transition-colors duration-300 overflow-hidden border-y dark:border-white/5">
                <div className="container mx-auto px-4 md:px-6 relative">
                    <div className="flex flex-col md:flex-row items-center justify-between gap-12">
                        <div className="z-10 text-center md:text-left">
                            <h2 className="text-3xl font-bold mb-4">Plays well with others.</h2>
                            <p className="text-slate-600 dark:text-slate-400 mb-8 max-w-md transition-colors duration-300">Integrate Winnow into your existing workflow in minutes. Support for all major dev tools.</p>
                            <div className="flex flex-wrap gap-6 justify-center md:justify-start grayscale opacity-50 hover:grayscale-0 hover:opacity-100 transition-all duration-500">
                                <Slack className="h-8 w-8" />
                                <Github className="h-8 w-8" />
                                <div className="font-bold text-2xl tracking-tighter">Linear</div>
                                <div className="font-bold text-2xl tracking-tighter">Jira</div>
                            </div>
                        </div>
                        <div className="relative group">
                            <div className="absolute -inset-1 bg-gradient-to-r from-blue-600 to-purple-600 rounded-full opacity-20 group-hover:opacity-40 blur transition duration-1000 group-hover:duration-200"></div>
                            <Link to="/integrations" className="relative px-8 py-4 bg-slate-900 dark:bg-white text-white dark:text-slate-950 rounded-full font-bold flex items-center gap-2 hover:bg-slate-800 dark:hover:bg-slate-50 transition-all transition-colors duration-300">
                                View Integrations <ArrowRight className="h-4 w-4" />
                            </Link>
                        </div>
                    </div>
                </div>
            </section>

            {/* Content Moderation Section */}
            <section className="py-20 md:py-32">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="flex flex-col lg:flex-row items-center gap-16">
                        <div className="lg:w-1/2 order-2 lg:order-1">
                            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-bold mb-6">
                                <Eye className="h-3 w-3" /> AI-DRIVEN SAFETY
                            </div>
                            <h2 className="text-3xl md:text-4xl font-bold mb-6">Proactive Content Moderation</h2>
                            <p className="text-lg text-muted-foreground leading-relaxed mb-8">
                                Protect your community and your brand. Winnow's AI automatically scans incoming media and text for harmful content before it ever reaches your team.
                            </p>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 text-left">
                                <div className="space-y-3">
                                    <div className="h-10 w-10 rounded-xl bg-slate-50 dark:bg-slate-900 border flex items-center justify-center shadow-sm">
                                        <Video className="h-5 w-5 text-indigo-500" />
                                    </div>
                                    <h3 className="font-bold">Multi-modal Analysis</h3>
                                    <p className="text-sm text-muted-foreground">Automatically detect NSFW content, violence, or sensitive material in images and videos with 99% accuracy.</p>
                                </div>
                                <div className="space-y-3">
                                    <div className="h-10 w-10 rounded-xl bg-slate-50 dark:bg-slate-900 border flex items-center justify-center shadow-sm">
                                        <MessageSquare className="h-5 w-5 text-emerald-500" />
                                    </div>
                                    <h3 className="font-bold">Natural Language Guardrails</h3>
                                    <p className="text-sm text-muted-foreground">Filter toxic language, detect PII, and prevent spam directly at the ingestion layer.</p>
                                </div>
                            </div>
                        </div>
                        <div className="lg:w-1/2 order-1 lg:order-2">
                            <div className="relative group">
                                <div className="absolute -inset-4 bg-gradient-to-r from-primary to-indigo-500 rounded-[2rem] opacity-10 group-hover:opacity-20 blur-2xl transition duration-500" />
                                <ScanningSimulator />
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* Technical Specs */}
            <section className="py-20 md:py-32 bg-slate-50 dark:bg-slate-900/50">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="text-center max-w-2xl mx-auto mb-16">
                        <h2 className="text-3xl font-bold mb-4">Built for Scale</h2>
                        <p className="text-muted-foreground">The performance and reliability you need to monitor applications serving millions of users.</p>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
                        {technicalSpecs.map((spec, i) => (
                            <div key={i} className="p-8 rounded-3xl border bg-card shadow-sm hover:shadow-xl hover:-translate-y-1 transition-all duration-300">
                                <spec.icon className="h-8 w-8 text-primary mb-6" />
                                <h3 className="font-bold text-xl mb-2">{spec.title}</h3>
                                <p className="text-sm text-muted-foreground leading-relaxed">{spec.description}</p>
                            </div>
                        ))}
                    </div>
                </div>
            </section>

            {/* Enterprise Hierarchy & Security Section */}
            <section className="py-20 md:py-32">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-24">
                        {/* Org Hierarchy */}
                        <div className="space-y-8">
                            <div>
                                <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400 text-xs font-bold mb-6">
                                    <Users2 className="h-3 w-3" /> SCALABLE GOVERNANCE
                                </div>
                                <h2 className="text-3xl font-bold mb-4">Architected for the Enterprise</h2>
                                <p className="text-muted-foreground text-lg leading-relaxed mb-8">
                                    Structure your workflow to match your organization. Winnow's multi-tenant architecture is built for the largest engineering teams.
                                </p>
                            </div>
                            <div className="space-y-6">
                                <div className="p-6 rounded-2xl border bg-card hover:bg-accent/5 transition-colors">
                                    <h3 className="font-bold text-xl mb-2 italic">Org &rarr; Team &rarr; Project</h3>
                                    <p className="text-muted-foreground text-sm">Clear ownership and data isolation for multiple business units, ensuring your teams only see the reports relevant to them.</p>
                                </div>
                                <div className="p-6 rounded-2xl border bg-card hover:bg-accent/5 transition-colors">
                                    <h3 className="font-bold text-xl mb-2">Role-Based Access Control</h3>
                                    <p className="text-muted-foreground text-sm">Fine-grained permissions from administrators to read-only viewers, keeping your sensitive crash data secure.</p>
                                </div>
                            </div>
                        </div>

                        {/* Security */}
                        <div className="space-y-8">
                            <div>
                                <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 text-xs font-bold mb-6">
                                    <Lock className="h-3 w-3" /> HARDENED BY DESIGN
                                </div>
                                <h2 className="text-3xl font-bold mb-4">Zero-Compromise Security</h2>
                                <p className="text-muted-foreground text-lg leading-relaxed mb-8">
                                    Security isn't an afterthought. We've built enterprise-grade features into the core of Winnow.
                                </p>
                            </div>
                            <div className="space-y-6">
                                <div className="p-6 rounded-2xl border bg-card hover:bg-accent/5 transition-colors">
                                    <div className="flex items-center justify-between mb-4">
                                        <h3 className="font-bold text-xl italic">Dual-Slot Key Rotation</h3>
                                        <div className="h-8 w-8 rounded-lg bg-red-50 dark:bg-red-900/30 flex items-center justify-center">
                                            <Key className="h-4 w-4 text-red-600" />
                                        </div>
                                    </div>
                                    <p className="text-muted-foreground text-sm leading-relaxed">
                                        Rotate your API keys without losing a single report. Our unique dual-slot architecture keeps your old key alive until your infrastructure is fully updated.
                                    </p>
                                </div>
                                <div className="p-6 rounded-2xl border bg-card hover:bg-accent/5 transition-colors">
                                    <h3 className="font-bold text-xl mb-2 italic">Encryption at Rest</h3>
                                    <p className="text-muted-foreground text-sm leading-relaxed">
                                        All sensitive data, including API key hashes and PII, is encrypted using industry-standard protocols before ever touching the disk.
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            {/* CTA Section */}
            <CTA />
        </div>
    );
}
