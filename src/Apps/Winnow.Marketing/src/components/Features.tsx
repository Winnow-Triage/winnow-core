import { Microscope, GitMerge, Kanban } from 'lucide-react';

const features = [
    {
        title: "Context, Not Complaints",
        description: "Don't ask users for screenshots. Winnow automatically captures console logs, network failures, and browser environment details with every report.",
        icon: Microscope,
        color: "text-blue-500",
        bg: "bg-blue-100 dark:bg-blue-900/20"
    },
    {
        title: "AI-Powered Grouping",
        description: "100 users reporting the same bug shouldn't be 100 tickets. Winnow's vector engine groups duplicates instantly, showing you the impact scale.",
        icon: GitMerge,
        color: "text-purple-500",
        bg: "bg-purple-100 dark:bg-purple-900/20"
    },
    {
        title: "Instant Action",
        description: "Don't let bugs rot in an inbox. Export fully context-rich tickets to Trello, Jira, or GitHub with a single click.",
        icon: Kanban,
        color: "text-green-500",
        bg: "bg-green-100 dark:bg-green-900/20"
    }
];

export function Features() {
    return (
        <section className="relative bg-slate-50 dark:bg-slate-800/50 py-16 md:py-24 overflow-hidden">
            {/* Ambient Animated Glows */}
            <div className="absolute top-1/4 left-1/4 w-[2000px] h-[2000px] bg-blue-600/5 blur-[1000px] rounded-full animate-drift pointer-events-none"></div>
            <div className="absolute bottom-1/4 right-1/4 w-[2000px] h-[2000px] bg-purple-600/5 blur-[1000px] rounded-full animate-drift [animation-delay:-7s] pointer-events-none"></div>

            <div className="container mx-auto px-4 md:px-6 relative z-10">
                <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
                    {features.map((feature, index) => (
                        <div
                            key={index}
                            className="group relative flex flex-col items-center text-center overflow-hidden rounded-2xl p-8 transition-all hover:-translate-y-1"
                        >
                            {/* Hover Glow Effect */}
                            <div className="absolute -inset-full top-0 block h-full w-1/2 -skew-x-12 bg-gradient-to-r from-transparent to-white opacity-40 group-hover:animate-shine" />

                            <div className={`mb-6 inline-flex rounded-2xl ${feature.bg} p-4`}>
                                <feature.icon className={`h-12 w-12 ${feature.color}`} />
                            </div>
                            <h3 className="mb-3 text-2xl font-bold tracking-tight">
                                <span className="group-hover:text-brand-gradient transition-colors duration-300">
                                    {feature.title}
                                </span>
                            </h3>
                            <p className="text-muted-foreground leading-relaxed text-lg">
                                {feature.description}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
