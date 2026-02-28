import { useParams, Link, Navigate } from 'react-router-dom';
import { getPostBySlug } from '../data/blog';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { GradientText } from '../components/ui/GradientText';
import { SEOMeta } from '../components/SEOMeta';
import ReactMarkdown from 'react-markdown';
import { ArrowLeft, Clock, Tag, Calendar, User } from 'lucide-react';

export function BlogPost() {
    const { slug } = useParams<{ slug: string }>();
    const post = slug ? getPostBySlug(slug) : undefined;

    if (!post) {
        return <Navigate to="/blog" replace />;
    }

    // Filter out the leading H1 if it exists (usually redundant with the hero title)
    const content = post.content.replace(/^#\s+.*[\r\n]*/, '').trim();

    return (
        <div className="flex flex-col min-h-screen">
            <SEOMeta
                title={post.title}
                description={post.excerpt}
                ogImage={post.image}
                ogType="article"
            />
            {/* Post Hero */}
            <Section variant="slate" border="bottom" padding="xlarge" containerClassName="text-center">
                <HeroBackground />
                <div className="max-w-4xl mx-auto">
                    <Link
                        to="/blog"
                        className="inline-flex items-center text-sm font-bold text-primary mb-12 hover:-translate-x-1 transition-transform"
                    >
                        <ArrowLeft className="h-4 w-4 mr-2" /> Back to Engineering Blog
                    </Link>

                    <div className="flex items-center justify-center gap-6 text-xs font-bold uppercase tracking-[0.2em] text-blue-500 mb-8">
                        <span className="flex items-center gap-2"><Tag className="h-3.5 w-3.5" /> {post.category}</span>
                        <div className="h-1 w-1 rounded-full bg-slate-300" />
                        <span className="flex items-center gap-2 text-muted-foreground"><Clock className="h-3.5 w-3.5" /> {post.readTime}</span>
                    </div>

                    <h1 className="text-5xl md:text-7xl font-extrabold tracking-tight mb-10 leading-[1.1]">
                        <GradientText>{post.title}</GradientText>
                    </h1>

                    <div className="flex flex-wrap items-center justify-center gap-y-4 gap-x-12 text-sm font-medium text-muted-foreground">
                        <span className="flex items-center gap-2.5">
                            <Calendar className="h-4 w-4 text-primary/60" />
                            {post.date}
                        </span>
                        {(post.authorName || post.author) && (
                            <span className="flex items-center gap-2.5">
                                <div className="h-6 w-6 rounded-full bg-primary/10 overflow-hidden flex items-center justify-center border border-primary/20 shadow-sm">
                                    {post.authorAvatar ? (
                                        <img src={post.authorAvatar} alt={post.authorName} className="h-full w-full object-cover" />
                                    ) : (
                                        <User className="h-3.5 w-3.5 text-primary" />
                                    )}
                                </div>
                                {post.authorName || post.author}
                            </span>
                        )}
                    </div>
                </div>
            </Section>

            {/* Post Content */}
            <Section padding="xlarge">
                <div className="max-w-3xl mx-auto">
                    <article className="prose prose-slate dark:prose-invert prose-xl max-w-none 
                        prose-headings:font-extrabold prose-headings:tracking-tight 
                        prose-a:text-primary prose-a:no-underline hover:prose-a:underline
                        prose-pre:bg-slate-900 prose-pre:border prose-pre:border-slate-800
                        prose-img:rounded-2xl prose-img:shadow-2xl">
                        <ReactMarkdown
                            components={{
                                h2: ({ node, ...props }) => (
                                    <h2 className="text-brand-gradient not-italic mt-16 mb-8" {...props} />
                                ),
                                h3: ({ node, ...props }) => (
                                    <h3 className="text-brand-gradient not-italic mt-12 mb-6" {...props} />
                                )
                            }}
                        >
                            {content}
                        </ReactMarkdown>
                    </article>

                    {/* Author Footer Card */}
                    <div className="mt-24 p-8 md:p-12 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 flex flex-col md:flex-row items-center md:items-start gap-8">
                        <div className="h-24 w-24 rounded-2xl bg-primary/10 overflow-hidden flex items-center justify-center flex-shrink-0 border border-primary/20 shadow-xl">
                            {post.authorAvatar ? (
                                <img src={post.authorAvatar} alt={post.authorName} className="h-full w-full object-cover" />
                            ) : (
                                <User className="h-12 w-12 text-primary" />
                            )}
                        </div>
                        <div className="text-center md:text-left">
                            <h4 className="text-2xl font-bold mb-1">Written by {post.authorName || post.author || 'Winnow Team'}</h4>
                            {post.authorTitle && (
                                <p className="text-primary font-medium text-sm mb-4 uppercase tracking-wider">{post.authorTitle}</p>
                            )}
                            <p className="text-muted-foreground leading-relaxed mb-6 max-w-xl">
                                Engineering and product insights from the team building the future of semantic triage and high-scale observability.
                            </p>
                            <div className="flex flex-wrap justify-center md:justify-start gap-4">
                                <Link
                                    to="/blog"
                                    className="inline-flex items-center justify-center rounded-full bg-primary px-6 py-2.5 text-sm font-bold text-primary-foreground shadow transition-all hover:bg-primary/90 hover:scale-[1.02]"
                                >
                                    More from the Blog
                                </Link>
                                <Link
                                    to="/contact"
                                    className="inline-flex items-center justify-center rounded-full bg-background border border-primary/20 px-6 py-2.5 text-sm font-bold text-primary transition-all hover:bg-accent hover:scale-[1.02]"
                                >
                                    Get in Touch
                                </Link>
                            </div>
                        </div>
                    </div>
                </div>
            </Section>
        </div>
    );
}
