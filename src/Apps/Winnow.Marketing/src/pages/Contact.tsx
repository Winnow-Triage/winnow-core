import {
    Mail,
    MessageCircle,
    Send,
    LifeBuoy,
    Compass
} from 'lucide-react';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';

export function Contact() {
    return (
        <div className="flex flex-col min-h-screen">
            {/* Header */}
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <div className="max-w-3xl mx-auto">
                    <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                        Let's talk about <br />
                        <GradientText>your application stability.</GradientText>
                    </h1>
                    <p className="text-xl text-muted-foreground leading-relaxed">
                        Have questions about our AI clustering? Need help with an integration? Our team is here to help you get the most out of Winnow.
                    </p>
                </div>
            </Section>

            {/* Main Content */}
            <Section padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-16">
                    {/* Contact Form Placeholder */}
                    <Card isHoverable={false} className="lg:col-span-2 p-8 md:p-12">
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
                    </Card>

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

                        <Card isHoverable={false} className="p-6 bg-slate-50 dark:bg-slate-900/50 border border-dashed border-slate-200 dark:border-slate-800">
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
                        </Card>
                    </div>
                </div>
            </Section>
        </div>
    );
}
