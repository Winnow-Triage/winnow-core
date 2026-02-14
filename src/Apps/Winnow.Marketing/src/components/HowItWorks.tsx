import { Code, Zap, CheckSquare, ArrowRight } from 'lucide-react';

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
    return (
        <section className="bg-slate-50 dark:bg-slate-800/50 py-16 md:py-24 border-y border-slate-200 dark:border-slate-800">
            <div className="container mx-auto px-4 md:px-6">
                <div className="text-center mb-12">
                    <h2 className="text-3xl font-bold tracking-tighter md:text-4xl">From Chaos to Clarity in 3 Steps.</h2>
                </div>

                <div className="relative grid gap-8 md:grid-cols-3">
                    {/* Connecting Arrows for Desktop */}
                    <div className="hidden md:block absolute top-12 left-[20%] right-[20%] h-0.5 bg-gradient-to-r from-transparent via-slate-300 dark:via-slate-600 to-transparent z-0" />

                    {steps.map((step, index) => (
                        <div key={index} className="relative z-10 flex flex-col items-center text-center">
                            <div className={`mb-6 flex h-20 w-20 items-center justify-center rounded-2xl ${step.bg} shadow-lg ring-4 ring-white dark:ring-slate-950`}>
                                <step.icon className={`h-10 w-10 ${step.color}`} />
                            </div>
                            <h3 className="text-xl font-bold mb-3">{step.title}</h3>
                            <p className="text-muted-foreground leading-relaxed max-w-xs">
                                {step.description}
                            </p>

                            {/* Mobile Arrow */}
                            {index < steps.length - 1 && (
                                <div className="mt-8 md:hidden text-slate-300 dark:text-slate-600">
                                    <ArrowRight className="h-8 w-8 rotate-90" />
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
