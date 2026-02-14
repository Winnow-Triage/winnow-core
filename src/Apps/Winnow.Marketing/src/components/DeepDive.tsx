import { ArrowRight } from 'lucide-react';

export function DeepDive() {
    return (
        <section className="bg-slate-50 dark:bg-slate-900/50 py-16 md:py-24 border-y border-slate-200 dark:border-slate-800">
            <div className="container mx-auto px-4 md:px-6">
                {/* Row 1: Intelligence (Text Left, Image Right) */}
                <div className="flex flex-col md:flex-row items-center gap-12 mb-20 md:mb-32">
                    <div className="flex-1 space-y-6">
                        <h2 className="text-3xl font-bold tracking-tighter md:text-4xl text-slate-900 dark:text-slate-50">
                            Your Triage Assistant, <br />
                            <span className="text-primary">Not Just a Log.</span>
                        </h2>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            Most tools dump raw logs on you. Winnow uses Vector AI (Phi-3 and MiniLM) to analyze the semantic meaning of errors,
                            grouping "Login Failed" and "Auth Error" into the same cluster automatically.
                        </p>
                        <ul className="space-y-3 pt-4">
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
                    </div>
                    <div className="flex-1 w-full">
                        <div className="relative rounded-xl shadow-2xl overflow-hidden border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900">
                            {/* Browser Chrome (Simplified) */}
                            <div className="h-8 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 flex items-center px-4 space-x-2">
                                <div className="h-3 w-3 rounded-full bg-slate-300 dark:bg-slate-600"></div>
                                <div className="h-3 w-3 rounded-full bg-slate-300 dark:bg-slate-600"></div>
                            </div>
                            <div className="aspect-[4/3] bg-slate-100 dark:bg-slate-950 flex items-center justify-center relative">
                                <img
                                    src="/dashboard-mockup-light.png"
                                    className="w-full h-full object-cover dark:hidden"
                                    alt="Cluster View Light"
                                />
                                <img
                                    src="/dashboard-mockup-dark.png"
                                    className="w-full h-full object-cover hidden dark:block"
                                    alt="Cluster View Dark"
                                />
                                {/* Overlay to focus on list if needed, or just let the image speak */}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Row 2: Context (Image Left, Text Right) */}
                <div className="flex flex-col md:flex-row-reverse items-center gap-12">
                    <div className="flex-1 space-y-6">
                        <h2 className="text-3xl font-bold tracking-tighter md:text-4xl text-slate-900 dark:text-slate-50">
                            Full Stack Context. <br />
                            <span className="text-primary">From React to Godot.</span>
                        </h2>
                        <p className="text-lg text-muted-foreground leading-relaxed">
                            Whether it's a JavaScript console error or a C# Exception in Unity/Godot, Winnow captures the full stack trace, variable state, and session logs.
                        </p>
                        <div className="pt-4">
                            <a href="#" className="inline-flex items-center font-medium text-primary hover:underline underline-offset-4">
                                Explore Integrations <ArrowRight className="ml-2 h-4 w-4" />
                            </a>
                        </div>
                    </div>
                    <div className="flex-1 w-full">
                        <div className="relative rounded-xl shadow-2xl overflow-hidden border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900">
                            {/* Simple Header */}
                            <div className="h-8 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 flex items-center px-4">
                                <span className="text-xs font-mono text-slate-500">Ticket #1249 - NullReferenceException</span>
                            </div>
                            <div className="aspect-[4/3] bg-slate-100 dark:bg-slate-950 flex items-center justify-center">
                                {/* Placeholder for Ticket Detail */}
                                <div className="text-center p-8">
                                    <div className="mb-4 inline-flex h-16 w-16 items-center justify-center rounded-2xl bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z" /><line x1="12" x2="12" y1="9" y2="13" /><line x1="12" x2="12.01" y1="17" y2="17" /></svg>
                                    </div>
                                    <p className="font-mono text-sm text-muted-foreground">
                                        NullReferenceException: Object reference not set to an instance of an object.<br />
                                        at GameController.Update() in GameController.cs:line 42
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
