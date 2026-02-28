import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { FileText, Code, Settings, Terminal, Zap, Puzzle } from 'lucide-react';

const docs = [
    {
        title: "Quick Start",
        description: "Get up and running with Winnow in under 5 minutes.",
        icon: Zap,
        link: "#"
    },
    {
        title: "JS SDK Reference",
        description: "Full documentation for our JavaScript and TypeScript SDKs.",
        icon: Code,
        link: "#"
    },
    {
        title: "Game Engine SDKs",
        description: "Guides for integrating with Unity and Godot (.NET).",
        icon: Terminal,
        link: "#"
    },
    {
        title: "API Reference",
        description: "Detailed documentation for our REST and WebSocket APIs.",
        icon: Settings,
        link: "#"
    },
    {
        title: "CLI Tool",
        description: "Manage projects and upload debug symbols from the command line.",
        icon: FileText,
        link: "#"
    },
    {
        title: "Integrations",
        description: "How to connect Slack, Jira, and GitHub to your projects.",
        icon: Puzzle,
        link: "#"
    }
];

export function Documentation() {
    return (
        <div className="flex flex-col min-h-screen">
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <div className="max-w-4xl mx-auto">
                    <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                        Developer <br />
                        <GradientText>Documentation.</GradientText>
                    </h1>
                    <p className="text-xl text-muted-foreground leading-relaxed max-w-2xl mx-auto">
                        Everything you need to integrate Winnow into your stack, from SDK references to detailed API guides.
                    </p>
                </div>
            </Section>

            <Section padding="large">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {docs.map((doc, i) => (
                        <Card key={i} className="p-8 hover:border-primary/50 transition-colors cursor-pointer group">
                            <div className="h-12 w-12 rounded-2xl bg-primary/5 border border-primary/10 flex items-center justify-center mb-6 group-hover:bg-primary group-hover:text-primary-foreground transition-all duration-300">
                                <doc.icon className="h-6 w-6" />
                            </div>
                            <h3 className="text-xl font-bold mb-3">{doc.title}</h3>
                            <p className="text-muted-foreground text-sm leading-relaxed">
                                {doc.description}
                            </p>
                        </Card>
                    ))}
                </div>
            </Section>
        </div>
    );
}
