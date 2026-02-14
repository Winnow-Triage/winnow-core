import { MoveRight } from 'lucide-react';

export function Hero() {
    return (
        <section className="container mx-auto grid items-center gap-6 pb-8 pt-6 md:py-10">
            <div className="flex max-w-[980px] flex-col items-center gap-2 mx-auto text-center">
                <h1 className="text-3xl font-extrabold leading-tight tracking-tighter md:text-5xl lg:text-6xl lg:leading-[1.1]">
                    Stop Drowning in <br className="hidden sm:inline" />
                    Duplicate Bug Reports.
                </h1>
                <p className="max-w-[750px] text-lg text-muted-foreground sm:text-xl">
                    Winnow uses AI to instantly group, analyze, and triage user feedback from your web apps.
                    Catch issues before they flood your inbox.
                </p>
                <div className="flex gap-4 mt-4">
                    <a
                        href="#playground"
                        className="inline-flex h-10 items-center justify-center rounded-md bg-primary px-8 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90"
                    >
                        Try the Playground <MoveRight className="ml-2 h-4 w-4" />
                    </a>
                </div>
            </div>

            <div className="mx-auto mt-8 w-full max-w-5xl relative">
                {/* Glow Effect */}
                <div className="absolute -inset-1 bg-gradient-to-r from-blue-600 to-purple-600 opacity-30 blur-2xl rounded-xl"></div>

                {/* Placeholder Image container */}
                <div className="relative rounded-xl border bg-card p-2 shadow-2xl skew-y-1">
                    <div className="aspect-[16/9] w-full rounded-lg bg-slate-100 dark:bg-slate-900 overflow-hidden">
                        <img
                            src="/dashboard-mockup-light.png"
                            alt="Winnow Dashboard Light Mode"
                            className="w-full h-full object-cover dark:hidden"
                        />
                        <img
                            src="/dashboard-mockup-dark.png"
                            alt="Winnow Dashboard Dark Mode"
                            className="w-full h-full object-cover hidden dark:block"
                        />
                    </div>
                </div>
            </div>
        </section>
    );
}
