import {
    Mail,
    MessageCircle,
    Send,
    LifeBuoy,
    Compass
} from 'lucide-react';

export function Contact() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Header */}
            <section className="relative py-24 md:py-32 overflow-hidden bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-white transition-colors duration-300 border-b">
                <div className="absolute top-0 left-1/2 -translate-x-1/2 w-full h-full max-w-7xl pointer-events-none">
                    <div className="absolute top-1/4 left-1/4 w-[2000px] h-[2000px] bg-blue-600/5 blur-[1000px] rounded-full animate-drift pointer-events-none"></div>
                    <div className="absolute bottom-1/4 right-1/4 w-[2000px] h-[2000px] bg-purple-600/5 blur-[1000px] rounded-full animate-drift [animation-delay:-7s] pointer-events-none"></div>
                </div>
                <div className="absolute inset-0 bg-grid-slate-950/[0.02] dark:bg-grid-white/[0.02] pointer-events-none" />
                <div className="container mx-auto px-4 md:px-6 relative z-10 text-center">
                    <div className="max-w-3xl mx-auto">
                        <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                            Let's talk about <br />
                            <span className="text-brand-gradient italic">your application stability.</span>
                        </h1>
                        <p className="text-xl text-muted-foreground leading-relaxed">
                            Have questions about our AI clustering? Need help with an integration? Our team is here to help you get the most out of Winnow.
                        </p>
                    </div>
                </div>
            </section>

            {/* Main Content */}
            <section className="py-20 md:py-32">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-16">
                        {/* Contact Form Placeholder */}
                        <div className="lg:col-span-2 bg-card rounded-3xl border p-8 md:p-12 shadow-sm hover:shadow-xl transition-all duration-300">
                            <h2 className="text-2xl font-bold mb-8">Send us a message</h2>
                            <form className="space-y-6" onSubmit={(e) => e.preventDefault()}>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    <div className="space-y-2">
                                        <label className="text-sm font-medium">First Name</label>
                                        <input type="text" className="w-full px-4 py-2 rounded-lg border bg-background" placeholder="Jane" />
                                    </div>
                                    <div className="space-y-2">
                                        <label className="text-sm font-medium">Last Name</label>
                                        <input type="text" className="w-full px-4 py-2 rounded-lg border bg-background" placeholder="Doe" />
                                    </div>
                                </div>
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Email Address</label>
                                    <input type="email" className="w-full px-4 py-2 rounded-lg border bg-background" placeholder="jane@example.com" />
                                </div>
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Message</label>
                                    <textarea className="w-full px-4 py-2 rounded-lg border bg-background min-h-[150px]" placeholder="Tell us about your project..."></textarea>
                                </div>
                                <button className="w-full md:w-auto px-8 py-3 bg-primary text-primary-foreground rounded-lg font-bold flex items-center justify-center gap-2 hover:bg-primary/90 transition-colors">
                                    Send Message <Send className="h-4 w-4" />
                                </button>
                            </form>
                        </div>

                        {/* Sidebar Info */}
                        <div className="space-y-12">
                            <div>
                                <h3 className="text-xl font-bold mb-6 flex items-center gap-2">
                                    <LifeBuoy className="h-5 w-5 text-blue-500" /> Support Channels
                                </h3>
                                <div className="space-y-6">
                                    <div className="flex gap-4">
                                        <div className="h-10 w-10 shrink-0 rounded-lg bg-blue-100 dark:bg-blue-900/20 flex items-center justify-center">
                                            <Mail className="h-5 w-5 text-blue-500" />
                                        </div>
                                        <div>
                                            <p className="font-semibold italic">Email Support</p>
                                            <p className="text-sm text-muted-foreground">support@winnowtriage.com</p>
                                        </div>
                                    </div>
                                    <div className="flex gap-4">
                                        <div className="h-10 w-10 shrink-0 rounded-lg bg-purple-100 dark:bg-purple-900/20 flex items-center justify-center">
                                            <MessageCircle className="h-5 w-5 text-purple-500" />
                                        </div>
                                        <div>
                                            <p className="font-semibold italic">Join our Slack</p>
                                            <p className="text-sm text-muted-foreground">Community support and updates.</p>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="p-6 rounded-2xl bg-slate-50 dark:bg-slate-900/50 border border-dashed border-slate-200 dark:border-slate-800">
                                <h3 className="text-lg font-bold mb-4 flex items-center gap-2">
                                    <Compass className="h-4 w-4" /> Office Hours
                                </h3>
                                <div className="space-y-2 text-sm text-muted-foreground">
                                    <div className="flex justify-between">
                                        <span>Mon - Fri</span>
                                        <span>9am - 5pm CST</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Sat - Sun</span>
                                        <span>Emergency Only</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    );
}
