import { ArrowRight, Trello, Github, KanbanSquare } from 'lucide-react';
import type { ReactNode } from 'react';
import { LightboxImage } from './ui/LightboxImage';

interface DeepDiveRowProps {
    title: ReactNode;
    description: string;
    imageSrc: string;
    imageDarkSrc?: string;
    imageAlt: string;
    reverse?: boolean; // If true, Image Left / Text Right. Default is Text Left / Image Right
    children?: ReactNode; // For badges, lists, or links
    theme?: 'default' | 'warning';
}

function DeepDiveRow({
    title,
    description,
    imageSrc,
    imageDarkSrc,
    imageAlt,
    reverse = false,
    children,
    theme = 'default'
}: DeepDiveRowProps) {
    const isWarning = theme === 'warning';

    // Theme configurations
    const borderColor = isWarning ? 'border-amber-200 dark:border-amber-900/50' : 'border-slate-200 dark:border-slate-700';
    const chromeBg = isWarning ? 'bg-amber-50 dark:bg-slate-800' : 'bg-slate-50 dark:bg-slate-800';
    const chromeBorder = isWarning ? 'border-amber-100 dark:border-amber-900/30' : 'border-slate-200 dark:border-slate-700';

    return (
        <div className={`flex flex-col ${reverse ? 'md:flex-row-reverse' : 'md:flex-row'} items-center gap-12 md:gap-24`}>
            {/* Text Content */}
            <div className="flex-1 space-y-6 w-full">
                <h2 className="text-3xl font-bold tracking-tighter md:text-4xl text-slate-900 dark:text-slate-50">
                    {title}
                </h2>
                <p className="text-lg text-muted-foreground leading-relaxed">
                    {description}
                </p>
                {children && <div className="pt-4">{children}</div>}
            </div>

            {/* Image Content */}
            <div className="flex-1 w-full relative">
                {/* Exact Hero-style Glow Effect */}
                <div className={`absolute -inset-1 opacity-20 blur-3xl rounded-xl -z-10 bg-gradient-to-r from-blue-600 to-purple-600`} />

                <div className={`relative rounded-xl shadow-2xl overflow-hidden border ${borderColor} bg-white dark:bg-slate-900`}>
                    {/* Browser Chrome */}
                    <div className={`h-8 border-b ${chromeBorder} ${chromeBg} flex items-center px-4 space-x-2`}>
                        <div className="h-3 w-3 rounded-full bg-slate-300 dark:bg-slate-600"></div>
                        <div className="h-3 w-3 rounded-full bg-slate-300 dark:bg-slate-600"></div>
                    </div>
                    {/* Image Container */}
                    <div className="bg-slate-100 dark:bg-slate-950 flex items-center justify-center relative">
                        {imageDarkSrc ? (
                            <>
                                <LightboxImage
                                    src={imageSrc}
                                    alt={imageAlt}
                                    className="w-full h-auto dark:hidden"
                                />
                                <LightboxImage
                                    src={imageDarkSrc}
                                    alt={imageAlt}
                                    className="w-full h-auto hidden dark:block"
                                />
                            </>
                        ) : (
                            <LightboxImage
                                src={imageSrc}
                                alt={imageAlt}
                                className="w-full h-auto"
                            />
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

export function DeepDive() {
    return (
        <section className="relative bg-slate-50 dark:bg-slate-900/50 py-16 md:py-24 border-y border-slate-200 dark:border-slate-800 overflow-hidden">
            <div className="container mx-auto px-4 md:px-6 space-y-20 md:space-y-32 relative z-10">

                {/* Row 1: Intelligence */}
                <DeepDiveRow
                    title={<>Your Triage Assistant, <br /><span className="text-brand-gradient">Not Just a Log.</span></>}
                    description='Most tools dump raw logs on you. Winnow uses Vector AI (Phi-3 and MiniLM) to analyze the semantic meaning of errors, like grouping "Login Failed" and "Auth Error" into the same cluster automatically.'
                    imageSrc="/clusters-dashboard-light.png"
                    imageDarkSrc="/clusters-dashboard-dark.png"
                    imageAlt="Cluster View"
                >
                    <ul className="space-y-3">
                        <li className="flex items-center text-muted-foreground">
                            <div className="h-2 w-2 rounded-full bg-blue-500 mr-3" />
                            Semantic Grouping
                        </li>
                        <li className="flex items-center text-muted-foreground">
                            <div className="h-2 w-2 rounded-full bg-purple-500 mr-3" />
                            Noise Reduction
                        </li>
                        <li className="flex items-center text-muted-foreground">
                            <div className="h-2 w-2 rounded-full bg-green-500 mr-3" />
                            Root Cause Analysis
                        </li>
                    </ul>
                </DeepDiveRow>

                {/* Row 2: Context */}
                <DeepDiveRow
                    title={<>Full Stack Context. <br /><span className="text-brand-gradient">From React to Godot.</span></>}
                    description="Whether it's a JavaScript console error or a C# Exception in Unity/Godot, Winnow captures the full stack trace, variable state, and session logs."
                    imageSrc="/dashboard-mockup-light.png"
                    imageDarkSrc="/dashboard-mockup-dark.png"
                    imageAlt="Report Detail View"
                    reverse={true}
                >
                    <a href="#" className="inline-flex items-center font-medium  text-slate-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors duration-300 hover:underline underline-offset-4">
                        Explore Integrations <ArrowRight className="ml-2 h-4 w-4" />
                    </a>
                </DeepDiveRow>

                {/* Row 3: Trust */}
                <DeepDiveRow
                    title={<>Trust, but <span className="text-amber-500">Verify.</span></>}
                    description="We know AI hallucinates. That's why Winnow gives you a 'Tinder-style' review interface. Swipe left to reject a bad merge, swipe right to confirm a cluster. You are always the pilot; AI is just the co-pilot."
                    imageSrc="/triage-review-light.png"
                    imageDarkSrc="/triage-review-dark.png"
                    imageAlt="Merge Review Interface"
                >
                    <div className="flex items-center space-x-4">
                        <div className="flex items-center text-sm font-medium text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 px-3 py-1 rounded-full border border-amber-200 dark:border-amber-800">
                            <span className="w-2 h-2 rounded-full bg-amber-500 mr-2 animate-pulse"></span>
                            Human in the Loop
                        </div>
                    </div>
                </DeepDiveRow>

                {/* Row 4: Integrations */}
                <DeepDiveRow
                    title={<>Don't Change <br /><span className="text-brand-gradient">Your Workflow.</span></>}
                    description="Winnow isn't another project management tool. It's the filter. Once you've triaged a cluster, export it to Trello, Jira, or GitHub with a single click. We send the logs, the context, and the summary right to your engineers."
                    imageSrc="/triage-export-light.png"
                    imageDarkSrc="/triage-export-dark.png"
                    imageAlt="Export to Trello Action"
                    reverse={true}
                >
                    <div className="flex items-center gap-6 text-slate-400">
                        <div className="flex flex-col items-center gap-2 hover:text-[#0079BF] transition-colors">
                            <Trello className="h-8 w-8" />
                            <span className="text-xs font-medium">Trello</span>
                        </div>
                        <div className="flex flex-col items-center gap-2 hover:text-[#0052CC] transition-colors">
                            <KanbanSquare className="h-8 w-8" />
                            <span className="text-xs font-medium">Jira</span>
                        </div>
                        <div className="flex flex-col items-center gap-2 hover:text-black dark:hover:text-white transition-colors">
                            <Github className="h-8 w-8" />
                            <span className="text-xs font-medium">GitHub</span>
                        </div>
                    </div>
                </DeepDiveRow>

            </div>
        </section>
    );
}
