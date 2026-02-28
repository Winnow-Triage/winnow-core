import { Section } from '../components/ui/Section';
import { HeroBackground } from '../components/ui/HeroBackground';

export function Privacy() {
    const lastUpdated = "February 27, 2026";

    return (
        <div className="flex flex-col min-h-screen">
            <Section variant="slate" border="bottom" padding="large">
                <HeroBackground />
                <div className="max-w-3xl mx-auto">
                    <h1 className="text-4xl md:text-6xl font-extrabold tracking-tight mb-6">Privacy Policy</h1>
                    <p className="text-xl text-muted-foreground leading-relaxed mb-4">Last Updated: {lastUpdated}</p>
                </div>
            </Section>

            {/* Footer Build CTA */}
            <Section border="top" variant="white" padding="large" className="flex-grow">
                <div className="max-w-3xl mx-auto">
                    <div className="prose dark:prose-invert max-w-none space-y-8 text-slate-700 dark:text-slate-300">
                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">1. Introduction</h2>
                            <p>
                                At Winnow Triage, LLC ("Winnow", "we", "us", or "our"), we respect your privacy and are committed to protecting your personal data. This Privacy Policy informs you about how we handle your personal data when you visit our website or use our services.
                            </p>
                        </section>

                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">2. The Data We Collect</h2>
                            <p>
                                We may collect, use, store, and transfer different kinds of personal data about you, including:
                            </p>
                            <ul className="list-disc pl-6 space-y-2">
                                <li><strong>Identity Data:</strong> includes first name, last name, and username.</li>
                                <li><strong>Contact Data:</strong> includes email address and billing address.</li>
                                <li><strong>Technical Data:</strong> includes IP addresses, login data, browser type and version, and operating system.</li>
                                <li><strong>Usage Data:</strong> includes information about how you use our website and services.</li>
                            </ul>
                        </section>

                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">3. How We Use Your Data</h2>
                            <p>
                                We use your data primarily to provide and improve our services, including:
                            </p>
                            <ul className="list-disc pl-6 space-y-2">
                                <li>To register you as a new customer.</li>
                                <li>To process crash reports and provide AI clustering services.</li>
                                <li>To manage our relationship with you (e.g., support requests).</li>
                                <li>To improve our website and marketing efforts.</li>
                            </ul>
                        </section>

                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">4. AI and Crash Report Data</h2>
                            <p>
                                When you use Winnow to ingest crash reports, we process technical data (logs, stack traces, screenshots) to provide our core services. This data is used solely for the purpose of error triage and is stored securely. AI processing for clustering is performed on anonymized vector embeddings where possible.
                            </p>
                        </section>

                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">5. Data Security</h2>
                            <p>
                                We have put in place appropriate security measures to prevent your personal data from being accidentally lost, used, or accessed in an unauthorized way.
                            </p>
                        </section>

                        <section>
                            <h2 className="text-2xl font-bold text-foreground mb-4">6. Contact Us</h2>
                            <p>
                                If you have any questions about this Privacy Policy, please contact us at privacy@winnowtriage.com.
                            </p>
                        </section>
                    </div>
                </div>
            </Section>
        </div>
    );
}
