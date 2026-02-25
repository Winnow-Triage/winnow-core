import { ArrowRight } from 'lucide-react';

export function CTA() {
    return (
        <section className="bg-white dark:bg-slate-950 py-24 sm:py-32">
            <div className="container mx-auto px-4 md:px-6">
                <div className="relative mx-auto max-w-5xl overflow-hidden bg-gradient-to-r from-blue-600 to-purple-600 rounded-3xl shadow-2xl px-6 py-16 sm:p-20 text-center">
                    {/* Subtle texture/glows for depth */}
                    <div className="absolute top-0 right-0 -translate-y-1/2 translate-x-1/2 w-[500px] h-[500px] bg-white/10 blur-[100px] rounded-full pointer-events-none" />
                    <div className="absolute bottom-0 left-0 translate-y-1/2 -translate-x-1/2 w-[500px] h-[500px] bg-black/10 blur-[100px] rounded-full pointer-events-none" />

                    <div className="relative z-10">
                        <h2 className="text-3xl font-extrabold tracking-tighter sm:text-4xl md:text-5xl lg:text-6xl text-white mb-6">
                            Ready to stop drowning in bugs?
                        </h2>
                        <p className="max-w-[800px] mx-auto text-lg md:text-xl text-blue-100 mb-10 leading-relaxed font-medium">
                            Join hundreds of engineering teams using Winnow to triage at the speed of AI.
                            Start your free trial today and fix what matters.
                        </p>

                        <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
                            <a
                                href="/pricing"
                                className="h-12 px-8 rounded-full bg-white text-blue-700 font-bold hover:scale-105 transition-all shadow-xl flex items-center group no-underline"
                            >
                                Get Started for Free
                                <ArrowRight className="ml-2 h-5 w-5 group-hover:translate-x-1 transition-transform" />
                            </a>
                            <button className="h-12 px-8 rounded-full border border-white text-white font-semibold hover:bg-white/10 transition-all">
                                Schedule a Demo
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
