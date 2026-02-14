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
        <section className="bg-slate-50 dark:bg-slate-800/50 py-16 md:py-24">
            <div className="container mx-auto px-4 md:px-6">
                <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
                    {features.map((feature, index) => (
                        <div
                            key={index}
                            className="group relative overflow-hidden rounded-2xl border bg-background p-8 transition-all hover:-translate-y-1 hover:shadow-lg dark:hover:shadow-primary/5"
                        >
                            {/* Hover Glow Effect */}
                            <div className="absolute -right-10 -top-10 h-32 w-32 rounded-full bg-primary/5 opacity-0 transition-opacity group-hover:opacity-100 blur-3xl" />

                            <div className={`mb-4 inline-flex rounded-lg ${feature.bg} p-3`}>
                                <feature.icon className={`h-6 w-6 ${feature.color}`} />
                            </div>
                            <h3 className="mb-2 text-xl font-bold tracking-tight">{feature.title}</h3>
                            <p className="text-muted-foreground leading-relaxed">
                                {feature.description}
                            </p>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
}
