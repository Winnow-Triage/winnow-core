import { MoveRight } from 'lucide-react';
import { LightboxImage } from './ui/LightboxImage';

export function Hero() {
    return (
        <section className="container mx-auto grid items-center gap-6 pb-20 pt-32 md:py-32">
            <div className="flex max-w-[980px] flex-col items-center gap-2 mx-auto text-center">
                <h1 className="text-3xl font-extrabold leading-tight tracking-tighter md:text-5xl lg:text-6xl lg:leading-[1.1]">
                    Stop Drowning in <br className="hidden sm:inline" />
                    Duplicate Bug Reports.
                </h1>
                <p className="max-w-[750px] text-lg text-muted-foreground sm:text-xl">
                    Winnow uses AI to instantly group, analyze, and triage user feedback from your apps.
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

            <div className="mx-auto mt-16 w-full max-w-5xl relative perspective-[2000px]">
                {/* Glow Effect */}
                <div className="absolute -inset-1 bg-gradient-to-r from-blue-600 to-purple-600 opacity-20 blur-3xl rounded-xl"></div>

                {/* Browser Chrome Container */}
                <div className="relative rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900 shadow-2xl rotate-x-6 transition-transform duration-500 hover:rotate-x-0 overflow-hidden">
                    {/* Window Controls Bar */}
                    <div className="h-11 border-b border-slate-200 dark:border-slate-700 flex items-center px-4 space-x-2 bg-slate-100 dark:bg-slate-800">
                        <div className="h-3 w-3 rounded-full bg-red-400"></div>
                        <div className="h-3 w-3 rounded-full bg-yellow-400"></div>
                        <div className="h-3 w-3 rounded-full bg-green-400"></div>
                    </div>

                    {/* Image Content */}
                    <div className="aspect-[16/9] w-full bg-slate-100 dark:bg-slate-900">
                        <LightboxImage
                            src="/triage-dashboard-light.png"
                            alt="Winnow Dashboard Light Mode"
                            className="w-full h-auto dark:hidden rounded-b-xl"
                            imageClassName="rounded-b-xl object-cover"
                        />
                        <LightboxImage
                            src="/triage-dashboard-dark.png"
                            alt="Winnow Dashboard Dark Mode"
                            className="w-full h-auto hidden dark:block rounded-b-xl"
                            imageClassName="rounded-b-xl object-cover"
                        />
                    </div>
                </div>
            </div>
        </section>
    );
}