import { ArrowRight } from 'lucide-react';

export function CTA() {
    return (
        <section className="bg-white dark:bg-slate-950 py-24 sm:py-32 relative overflow-hidden">
            <div className="container mx-auto px-4 md:px-6 relative">
                <div className="relative mx-auto max-w-5xl overflow-hidden bg-slate-900 rounded-[2.5rem] shadow-2xl px-6 py-16 sm:p-20 text-center border border-white/10">
                    {/* Vibrant Background Mesh/Gradients */}
                    <div className="absolute inset-0 bg-gradient-to-br from-indigo-600 via-blue-600 to-purple-700 opacity-90" />
                    <div className="absolute -top-24 -right-24 w-96 h-96 bg-blue-400 blur-[120px] rounded-full opacity-50 animate-pulse" />
                    <div className="absolute -bottom-24 -left-24 w-96 h-96 bg-purple-500 blur-[120px] rounded-full opacity-50 animate-pulse transition-all duration-3000" />
                    <div className="absolute top-1/2 left-1/4 -translate-y-1/2 w-64 h-64 bg-indigo-300 blur-[100px] rounded-full opacity-20" />

                    {/* Subtle Scan Line for depth consistency */}
                    <div className="absolute inset-0 bg-grid-white/[0.05] [mask-image:linear-gradient(to_bottom,transparent,black,transparent)]" />

                    <div className="relative z-10">
                        <h2 className="text-3xl font-extrabold tracking-tighter sm:text-4xl md:text-5xl lg:text-7xl text-white mb-6 leading-[1.1]">
                            Ready to stop <br className="hidden sm:block" />
                            <span className="text-blue-200">drowning in bugs?</span>
                        </h2>
                        <p className="max-w-[800px] mx-auto text-lg md:text-xl text-blue-100/90 mb-10 leading-relaxed font-medium">
                            Join hundreds of engineering teams using Winnow to triage at the speed of AI.
                            Start your free trial today and fix what matters.
                        </p>

                        <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
                            <a
                                href="/pricing"
                                className="h-14 px-10 rounded-full bg-white text-blue-700 font-bold hover:scale-105 transition-all shadow-[0_0_40px_rgba(255,255,255,0.3)] flex items-center group no-underline"
                            >
                                Get Started for Free
                                <ArrowRight className="ml-2 h-5 w-5 group-hover:translate-x-1 transition-transform" />
                            </a>
                            <button className="h-14 px-10 rounded-full border-2 border-white/30 text-white font-bold hover:bg-white/10 hover:border-white/50 transition-all backdrop-blur-sm">
                                Schedule a Demo
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
