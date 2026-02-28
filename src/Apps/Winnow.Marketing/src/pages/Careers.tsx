import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { GradientText } from '../components/ui/GradientText';
import { Twitter, Mail, Rocket, Coffee, Shield, MessageSquare } from 'lucide-react';

export function Careers() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Hero Section */}
            <Section variant="slate" border="bottom" padding="xlarge" containerClassName="text-center">
                <HeroBackground />
                <div className="max-w-4xl mx-auto">
                    <h1 className="text-5xl md:text-8xl font-extrabold tracking-tight mb-8 leading-[1.05]">
                        Winnow is <br />
                        <GradientText>proudly solo-founded.</GradientText>
                    </h1>
                    <p className="text-xl md:text-2xl text-muted-foreground max-w-3xl mx-auto leading-relaxed">
                        We aren't a venture-backed growth machine. We are a bootstrapped, profitable, and developer-obsessed company built right here in Fort Worth, Texas.
                    </p>
                </div>
            </Section>

            {/* The Transparency Section */}
            <Section padding="large">
                <div className="max-w-4xl mx-auto">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-16 items-center">
                        <div>
                            <h2 className="text-3xl md:text-4xl font-bold mb-6 leading-tight">
                                Built by builders, <br />
                                <span className="text-primary">for builders.</span>
                            </h2>
                            <div className="space-y-6 text-lg text-muted-foreground leading-relaxed">
                                <p>
                                    Most observability tools are built by committee. They are designed for procurement departments, not for the developers who actually have to fix the bugs at 3 AM.
                                </p>
                                <p>
                                    Winnow is different. I started Winnow to solve my own burnout. I wanted a tool that actually understood my logs, not just one that stored them.
                                </p>
                            </div>
                        </div>
                        <div className="grid grid-cols-2 gap-6">
                            <div className="p-8 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 text-center">
                                <Rocket className="h-8 w-8 text-primary mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">100%</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Bootstrapped</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 text-center">
                                <Shield className="h-8 w-8 text-amber-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Independent</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">No Investor Pressure</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 text-center">
                                <Coffee className="h-8 w-8 text-indigo-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Solo</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Founded</div>
                            </div>
                            <div className="p-8 rounded-3xl bg-slate-50 dark:bg-slate-900 border border-slate-100 dark:border-slate-800 text-center">
                                <MessageSquare className="h-8 w-8 text-emerald-500 mx-auto mb-4" />
                                <div className="text-2xl font-bold mb-1">Direct</div>
                                <div className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Support</div>
                            </div>
                        </div>
                    </div>
                </div>
            </Section>

            {/* The "Hiring" Pivot Section */}
            <Section variant="muted" border="both" padding="large" containerClassName="text-center">
                <div className="max-w-3xl mx-auto">
                    <h2 className="text-3xl md:text-5xl font-bold mb-8">We aren't "hiring"— <br /><span className="text-primary italic">not in the corporate sense.</span></h2>
                    <p className="text-xl text-muted-foreground leading-relaxed mb-12">
                        Winnow is growing fast, but we are intentional about our scale. We aren't looking to fill "seats" or meet hiring quotas.
                        We are always, however, looking to connect with passionate developers, security researchers, and observability nerds who hate alert fatigue as much as we do.
                    </p>

                    <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
                        <a
                            href="https://twitter.com/stubbington"
                            target="_blank"
                            rel="noopener noreferrer"
                            className="w-full sm:w-auto inline-flex items-center justify-center gap-3 rounded-full bg-slate-950 px-8 py-4 text-sm font-bold text-white shadow-xl transition-all hover:bg-slate-800 hover:scale-[1.02] active:scale-[0.98]"
                        >
                            <Twitter className="h-5 w-5 fill-white" /> Follow the Journey
                        </a>
                        <a
                            href="mailto:james@winnowtriage.com"
                            className="w-full sm:w-auto inline-flex items-center justify-center gap-3 rounded-full bg-white px-8 py-4 text-sm font-bold text-slate-950 border border-slate-200 shadow-lg transition-all hover:bg-slate-50 hover:scale-[1.02] active:scale-[0.98]"
                        >
                            <Mail className="h-5 w-5" /> Say Hello
                        </a>
                    </div>
                </div>
            </Section>

            {/* Final Community CTA */}
            <Section padding="xlarge" containerClassName="text-center">
                <div className="max-w-4xl mx-auto">
                    <h3 className="text-2xl font-bold mb-6">Stay in the loop</h3>
                    <p className="text-muted-foreground mb-12 max-w-xl mx-auto">
                        Whether you're a potential future teammate or a satisfied user, we'd love to have you in our community.
                    </p>
                    <div className="flex justify-center gap-12">
                        <div className="flex flex-col items-center">
                            <div className="h-12 w-12 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center mb-4">
                                <Twitter className="h-6 w-6" />
                            </div>
                            <span className="text-sm font-bold tracking-wider uppercase">Twitter</span>
                        </div>
                        <div className="flex flex-col items-center">
                            <div className="h-12 w-12 rounded-full bg-slate-100 text-slate-600 flex items-center justify-center mb-4">
                                <Mail className="h-6 w-6" />
                            </div>
                            <span className="text-sm font-bold tracking-wider uppercase">Email List</span>
                        </div>
                    </div>
                </div>
            </Section>
        </div>
    );
}
