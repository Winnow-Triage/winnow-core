export function Terms() {
    const lastUpdated = "February 27, 2026";

    return (
        <div className="flex flex-col min-h-screen">
            <section className="py-20 md:py-32 bg-slate-50 dark:bg-slate-900/50">
                <div className="container mx-auto px-4 md:px-6">
                    <div className="max-w-3xl mx-auto">
                        <h1 className="text-4xl font-bold mb-4">Terms of Service</h1>
                        <p className="text-muted-foreground mb-8">Last Updated: {lastUpdated}</p>

                        <div className="prose dark:prose-invert max-w-none space-y-8 text-slate-700 dark:text-slate-300">
                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">1. Agreement to Terms</h2>
                                <p>
                                    By accessing or using Winnow's website and services, you agree to be bound by these Terms of Service. If you do not agree to all of these terms, do not use our services.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">2. Description of Service</h2>
                                <p>
                                    Winnow provides an AI-powered crash report triage and observability platform. Services include data ingestion, vector-based clustering, and integration with third-party development tools.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">3. User Accounts</h2>
                                <p>
                                    You are responsible for maintaining the confidentiality of your account credentials and for all activities that occur under your account. You must notify us immediately of any unauthorized use.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">4. Acceptable Use</h2>
                                <p>
                                    You agree not to use Winnow for any unlawful purpose or in any way that interrupts, damages, or impairs the service. You are solely responsible for the content of the crash reports you ingest.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">5. Intellectual Property</h2>
                                <p>
                                    The Winnow platform, including its AI models, algorithms, and interface, is the property of Winnow Triage, LLC and is protected by copyright and other intellectual property laws.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">6. Limitation of Liability</h2>
                                <p>
                                    To the maximum extent permitted by law, Winnow shall not be liable for any indirect, incidental, special, or consequential damages resulting from the use or inability to use our services.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">7. Termination</h2>
                                <p>
                                    We reserve the right to suspend or terminate your access to our services if you violate these terms or for any other reason at our sole discretion.
                                </p>
                            </section>

                            <section>
                                <h2 className="text-2xl font-bold text-foreground mb-4">8. Governing Law</h2>
                                <p>
                                    These terms shall be governed by and construed in accordance with the laws of the State of Texas, without regard to its conflict of law provisions.
                                </p>
                            </section>
                        </div>
                    </div>
                </div>
            </section>
        </div>
    );
}
