import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { ArrowRight, Clock, Tag } from 'lucide-react';

import { blogPosts } from '../data/blog';
import { Link } from 'react-router-dom';

export function Blog() {
    return (
        <div className="flex flex-col min-h-screen">
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                    Engineering <br />
                    <GradientText>the Future of Triage.</GradientText>
                </h1>
                <p className="text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                    Deep dives into vector similarity, high-scale telemetry, and the art of zero-noise observability.
                </p>
            </Section>

            <Section padding="xlarge">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                    {blogPosts.map((post, i) => (
                        <Card key={i} className="group flex flex-col h-full overflow-hidden">
                            <div className="aspect-video bg-slate-100 dark:bg-slate-800 relative overflow-hidden">
                                {/* Placeholder for blog image */}
                                <div className="absolute inset-0 flex items-center justify-center text-slate-400 font-bold uppercase tracking-tighter opacity-20 text-4xl rotate-12">
                                    Winnow Blog
                                </div>
                            </div>
                            <div className="p-6 flex flex-col flex-grow">
                                <div className="flex items-center gap-4 text-xs font-semibold uppercase tracking-wider text-blue-500 mb-4">
                                    <span className="flex items-center gap-1"><Tag className="h-3 w-3" /> {post.category}</span>
                                    <span className="flex items-center gap-1 text-muted-foreground"><Clock className="h-3 w-3" /> {post.readTime}</span>
                                </div>
                                <h3 className="text-xl font-bold mb-3 group-hover:text-primary transition-colors">{post.title}</h3>
                                <p className="text-muted-foreground text-sm leading-relaxed mb-6 flex-grow">
                                    {post.excerpt}
                                </p>
                                <div className="flex items-center justify-between mt-auto pt-6 border-t border-slate-100 dark:border-slate-800">
                                    <span className="text-xs text-muted-foreground">{post.date}</span>
                                    <Link to={`/blog/${post.slug}`} className="text-sm font-bold flex items-center text-primary group-hover:translate-x-1 transition-transform">
                                        Read Post <ArrowRight className="h-4 w-4 ml-1" />
                                    </Link>
                                </div>
                            </div>
                        </Card>
                    ))}
                </div>
            </Section>
        </div>
    );
}
