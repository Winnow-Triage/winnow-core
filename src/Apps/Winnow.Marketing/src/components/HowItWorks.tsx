import { Code, Zap, CheckSquare, ArrowRight } from 'lucide-react';
import { useState } from 'react';

const steps = [
    {
        title: "Capture",
        description: "Add the 2kb SDK to your app. We catch errors, rage clicks, and network fails automatically.",
        icon: Code,
        color: "text-blue-500",
        bg: "bg-blue-100 dark:bg-blue-900/20"
    },
    {
        title: "Compress",
        description: "Our Vector AI compares new reports against history. Duplicates are merged; new issues are flagged.",
        icon: Zap,
        color: "text-amber-500",
        bg: "bg-amber-100 dark:bg-amber-900/20"
    },
    {
        title: "Resolve",
        description: "Push a single, context-rich ticket to Jira or Trello. Your team fixes the root cause, not the symptom.",
        icon: CheckSquare,
        color: "text-green-500",
        bg: "bg-green-100 dark:bg-green-900/20"
    }
];

export function HowItWorks() {
    const [activeTab, setActiveTab] = useState<'js' | 'csharp'>('js');

    const Step1Icon = steps[0].icon;
    const Step2Icon = steps[1].icon;
    const Step3Icon = steps[2].icon;

    return (
        <section className="bg-white dark:bg-slate-900 py-16 md:py-24">
            <div className="container mx-auto px-4 md:px-6">
                <div className="text-center mb-16">
                    <h2 className="text-3xl font-bold tracking-tighter md:text-4xl">From Chaos to Clarity in 3 Steps.</h2>
                </div>

                <div className="relative grid gap-12 md:grid-cols-3">
                    {/* Connecting Arrows for Desktop - Adjusted z-index and position */}
                    <div className="hidden md:block absolute top-12 left-[20%] right-[20%] h-0.5 bg-gradient-to-r from-transparent via-slate-200 dark:via-slate-800 to-transparent z-0" />

                    {/* Step 1: Capture with Code Window */}
                    <div className="relative z-10 flex flex-col items-center text-center">
                        <div className={`mb-8 flex h-20 w-20 items-center justify-center rounded-2xl ${steps[0].bg} shadow-lg ring-4 ring-white dark:ring-slate-950`}>
                            <Step1Icon className={`h-10 w-10 ${steps[0].color}`} />
                        </div>
                        <h3 className="text-xl font-bold mb-3">{steps[0].title}</h3>
                        <p className="text-muted-foreground leading-relaxed max-w-xs mb-6">
                            {steps[0].description}
                        </p>

                        {/* Code Window */}
                        <div className="w-full max-w-[320px] mx-auto text-left rounded-lg overflow-hidden border border-slate-200 dark:border-slate-800 shadow-xl bg-slate-950">
                            {/* Tabs */}
                            <div className="flex border-b border-slate-800 bg-slate-900/50">
                                <button
                                    onClick={() => setActiveTab('js')}
                                    className={`px-4 py-2 text-xs font-medium transition-colors ${activeTab === 'js' ? 'bg-slate-800 text-blue-400 border-t-2 border-blue-400' : 'text-slate-500 hover:text-slate-300'}`}
                                >
                                    JavaScript
                                </button>
                                <button
                                    onClick={() => setActiveTab('csharp')}
                                    className={`px-4 py-2 text-xs font-medium transition-colors ${activeTab === 'csharp' ? 'bg-slate-800 text-purple-400 border-t-2 border-purple-400' : 'text-slate-500 hover:text-slate-300'}`}
                                >
                                    C# / .NET
                                </button>
                            </div>

                            {/* Code Content */}
                            <div className="p-4 bg-slate-950 font-mono text-xs overflow-x-auto">
                                {activeTab === 'js' ? (
                                    <>
                                        <span className="text-purple-400">import</span> {'{'} <span className="text-yellow-300">Winnow</span> {'}'} <span className="text-purple-400">from</span> <span className="text-green-400">'@winnow/sdk'</span>;
                                        <br />
                                        <br />
                                        <span className="text-purple-400">Winnow</span>.<span className="text-blue-400">init</span>({'{'}
                                        <div className="pl-4">
                                            <span className="text-slate-300">apiKey</span>: <span className="text-green-400">"..."</span>
                                        </div>
                                        {'}'});
                                    </>
                                ) : (
                                    <>
                                        <span className="text-blue-400">using</span> <span className="text-slate-300">Winnow.Sdk</span>;
                                        <br />
                                        <br />
                                        <span className="text-purple-400">Winnow</span>.<span className="text-blue-400">Start</span>(
                                        <div className="pl-4">
                                            <span className="text-blue-400">new</span> <span className="text-emerald-300">WinnowConfig</span> {'{'}
                                            <div className="pl-4">
                                                <span className="text-slate-300">ApiKey</span> = <span className="text-green-400">"..."</span>
                                            </div>
                                            {'}'}
                                        </div>
                                        );
                                    </>
                                )}
                            </div>
                        </div>

                        {/* Mobile Arrow */}
                        <div className="mt-8 md:hidden text-slate-300 dark:text-slate-600">
                            <ArrowRight className="h-8 w-8 rotate-90" />
                        </div>
                    </div>

                    {/* Step 2: Compress */}
                    <div className="relative z-10 flex flex-col items-center text-center mt-8 md:mt-0">
                        <div className={`mb-6 flex h-20 w-20 items-center justify-center rounded-2xl ${steps[1].bg} shadow-lg ring-4 ring-white dark:ring-slate-950`}>
                            <Step2Icon className={`h-10 w-10 ${steps[1].color}`} />
                        </div>
                        <h3 className="text-xl font-bold mb-3">{steps[1].title}</h3>
                        <p className="text-muted-foreground leading-relaxed max-w-xs">
                            {steps[1].description}
                        </p>

                        {/* Mobile Arrow */}
                        <div className="mt-8 md:hidden text-slate-300 dark:text-slate-600">
                            <ArrowRight className="h-8 w-8 rotate-90" />
                        </div>
                    </div>

                    {/* Step 3: Resolve */}
                    <div className="relative z-10 flex flex-col items-center text-center mt-8 md:mt-0">
                        <div className={`mb-6 flex h-20 w-20 items-center justify-center rounded-2xl ${steps[2].bg} shadow-lg ring-4 ring-white dark:ring-slate-950`}>
                            <Step3Icon className={`h-10 w-10 ${steps[2].color}`} />
                        </div>
                        <h3 className="text-xl font-bold mb-3">{steps[2].title}</h3>
                        <p className="text-muted-foreground leading-relaxed max-w-xs">
                            {steps[2].description}
                        </p>
                    </div>

                </div>
            </div>
        </section>
    );
}
