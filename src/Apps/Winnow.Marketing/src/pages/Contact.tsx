import {
    Mail,
    MessageCircle,
    Compass,
    MapPin,
    MessageSquare,
    LifeBuoy
} from 'lucide-react';
import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';
import { Card } from '../components/ui/Card';
import { GradientText } from '../components/ui/GradientText';
import { useState } from 'react';
import { CTA } from '../components/CTA';

export function Contact() {
    const [status, setStatus] = useState<'idle' | 'loading' | 'success' | 'error'>('idle');

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();
        setStatus('loading');

        const formData = new FormData(e.currentTarget);
        const data = {
            firstName: formData.get('firstName'),
            lastName: formData.get('lastName'),
            email: formData.get('email'),
            message: formData.get('message'),
        };

        try {
            const response = await fetch('http://localhost:5294/contact', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data),
            });

            if (response.ok) {
                setStatus('success');
            } else {
                setStatus('error');
            }
        } catch (error) {
            setStatus('error');
            console.error('Submission error:', error);
        }
    };

    return (
        <div className="flex flex-col min-h-screen">
            {/* Hero Section */}
            <Section variant="slate" border="bottom" padding="large" containerClassName="text-center">
                <HeroBackground />
                <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">
                    Get in <GradientText>touch.</GradientText>
                </h1>
                <p className="text-xl text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                    Have questions about our enterprise plans, custom integrations, or just want to say hi? We'd love to hear from you.
                </p>
            </Section>

            {/* Contact Form Section */}
            <Section padding="large">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-start">
                    <div className="space-y-12">
                        <div>
                            <h2 className="text-2xl font-bold mb-6">Contact Information</h2>
                            <div className="space-y-8">
                                <div className="flex items-start gap-4">
                                    <div className="h-10 w-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary shrink-0">
                                        <Mail className="h-5 w-5" />
                                    </div>
                                    <div>
                                        <h3 className="font-semibold">Email</h3>
                                        <p className="text-muted-foreground">support@winnow-triage.com</p>
                                    </div>
                                </div>
                                <div className="flex items-start gap-4">
                                    <div className="h-10 w-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary shrink-0">
                                        <MapPin className="h-5 w-5" />
                                    </div>
                                    <div>
                                        <h3 className="font-semibold">Office</h3>
                                        <p className="text-muted-foreground">Fort Worth, Texas</p>
                                    </div>
                                </div>
                                <div className="flex items-start gap-4">
                                    <div className="h-10 w-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary shrink-0">
                                        <MessageCircle className="h-5 w-5" />
                                    </div>
                                    <div>
                                        <h3 className="font-semibold">Community</h3>
                                        <p className="text-muted-foreground">Join our Slack for real-time support.</p>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <Card variant="default" className="p-8 md:p-12 bg-slate-50 dark:bg-slate-900/50 border-dashed">
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

                    <Card isHoverable={false} className="p-8 md:p-12">
                        {status === 'success' ? (
                            <div className="text-center py-12">
                                <div className="h-16 w-16 bg-emerald-500/10 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6">
                                    <MessageSquare className="h-8 w-8" />
                                </div>
                                <h2 className="text-2xl font-bold mb-4">Message Sent!</h2>
                                <p className="text-muted-foreground">Thank you for reaching out. A member of our team will get back to you shortly.</p>
                                <button
                                    onClick={() => setStatus('idle')}
                                    className="mt-8 text-primary font-bold flex items-center mx-auto hover:underline"
                                >
                                    Send another message
                                </button>
                            </div>
                        ) : (
                            <form onSubmit={handleSubmit} className="space-y-6">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    <div className="space-y-2">
                                        <label className="text-sm font-medium">First Name</label>
                                        <input name="firstName" required type="text" className="w-full px-4 py-2 rounded-lg border bg-background focus:ring-2 focus:ring-primary/20 outline-none transition-all" placeholder="First Name" />
                                    </div>
                                    <div className="space-y-2">
                                        <label className="text-sm font-medium">Last Name</label>
                                        <input name="lastName" required type="text" className="w-full px-4 py-2 rounded-lg border bg-background focus:ring-2 focus:ring-primary/20 outline-none transition-all" placeholder="Last Name" />
                                    </div>
                                </div>
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Email Address</label>
                                    <input name="email" required type="email" className="w-full px-4 py-2 rounded-lg border bg-background focus:ring-2 focus:ring-primary/20 outline-none transition-all" placeholder="email@company.com" />
                                </div>
                                <div className="space-y-2">
                                    <label className="text-sm font-medium">Message</label>
                                    <textarea name="message" required rows={5} className="w-full px-4 py-2 rounded-lg border bg-background focus:ring-2 focus:ring-primary/20 outline-none transition-all resize-none" placeholder="How can we help you?" />
                                </div>

                                {status === 'error' && (
                                    <p className="text-sm text-red-500 font-medium">Something went wrong. Please try again or email us directly.</p>
                                )}

                                <button
                                    disabled={status === 'loading'}
                                    type="submit"
                                    className="w-full h-12 rounded-lg bg-primary text-primary-foreground font-bold shadow-lg shadow-primary/20 transition-all hover:bg-primary/90 disabled:opacity-50"
                                >
                                    {status === 'loading' ? 'Sending...' : 'Send Message'}
                                </button>
                            </form>
                        )}
                    </Card>
                </div>
            </Section>

            <CTA />
        </div>
    );
}
