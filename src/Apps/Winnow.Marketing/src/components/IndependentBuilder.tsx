import { Shield, MapPin, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Section } from './ui/Section';
import { GradientText } from './ui/GradientText';

export function IndependentBuilder() {
    return (
        <Section padding="large" className="overflow-hidden">
            <div className="relative p-8 md:p-16 rounded-[2.5rem] bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 overflow-hidden">
                {/* Background Accent */}
                <div className="absolute top-0 right-0 -translate-y-1/2 translate-x-1/2 w-96 h-96 bg-primary/10 blur-[120px] rounded-full pointer-events-none" />
                <div className="absolute bottom-0 left-0 translate-y-1/2 -translate-x-1/2 w-64 h-64 bg-indigo-500/10 blur-[100px] rounded-full pointer-events-none" />

                <div className="relative z-10 flex flex-col lg:flex-row items-center justify-between gap-12">
                    <div className="max-w-2xl text-center lg:text-left">
                        <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary/10 text-primary text-xs font-bold uppercase tracking-wider mb-8">
                            <Shield className="h-3.5 w-3.5" />
                            No Investor Pressure
                        </div>
                        <h2 className="text-3xl md:text-5xl font-bold mb-6 leading-tight">
                            Independently built <br />
                            <GradientText>for builders.</GradientText>
                        </h2>
                        <p className="text-lg text-muted-foreground leading-relaxed mb-10">
                            Winnow is a self-funded, solo-founded observability lab. No corporate bloat, no VC-driven tracking, and no focus-group engineering. Just a tool built by a developer who got tired of alert fatigue.
                        </p>
                        <Link
                            to="/about"
                            className="inline-flex items-center gap-2 text-primary font-bold hover:gap-3 transition-all group"
                        >
                            Read our full story
                            <ArrowRight className="h-5 w-5 group-hover:translate-x-1 transition-transform" />
                        </Link>
                    </div>

                    <div className="hidden lg:block w-px h-32 bg-slate-200 dark:bg-slate-800" />

                    <div className="flex flex-col items-center lg:items-end text-center lg:text-right">
                        <div className="flex items-center gap-3 text-foreground font-bold text-xl mb-2">
                            <MapPin className="h-6 w-6 text-primary" />
                            Fort Worth, Texas
                        </div>
                        <div className="text-muted-foreground text-sm uppercase tracking-widest font-semibold">
                            Engineering Lab
                        </div>
                    </div>
                </div>
            </div>
        </Section>
    );
}
