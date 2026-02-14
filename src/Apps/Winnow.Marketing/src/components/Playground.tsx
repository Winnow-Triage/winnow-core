import { Bug, XCircle, AlertTriangle } from 'lucide-react';

export function Playground() {

    const trigger404 = async () => {
        try {
            await fetch('/api/ghost-endpoint');
        } catch (e) {
            console.error(e);
        }
    };

    const triggerReactError = () => {
        // We'll throw an error that the ErrorBoundary (or window.onerror) catches
        throw new Error("Synthetic React Rendering Error triggered from Playground");
    };

    const triggerConsoleError = () => {
        console.error("Payment Gateway Timeout: 504 - Gateway did not respond in time.");
    };

    return (
        <section id="playground" className="container mx-auto py-8 md:py-12 lg:py-24">
            <div className="mx-auto flex max-w-[58rem] flex-col items-center justify-center gap-4 text-center">
                <h2 className="font-bold text-3xl leading-[1.1] sm:text-3xl md:text-6xl">
                    Don't believe us? Break this page.
                </h2>
                <p className="max-w-[85%] leading-normal text-muted-foreground sm:text-lg sm:leading-7">
                    Interact with the buttons below to trigger real errors. Then, open the
                    <strong> Winnow Widget</strong> (bottom right) to report them instantly.
                </p>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-3 mt-8 w-full max-w-3xl">
                    <button
                        onClick={trigger404}
                        className="flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105"
                    >
                        <div className="mb-4 rounded-full bg-red-100 dark:bg-red-900/20 p-3">
                            <Bug className="h-6 w-6 text-red-600" />
                        </div>
                        <h3 className="font-bold">Trigger 404</h3>
                        <p className="text-sm text-muted-foreground mt-2">Fails a fetch request</p>
                    </button>

                    <button
                        onClick={() => {
                            // To actually crash React, we need to set state to throw during render.
                            // But for simplicity/safety in this demo, let's just log a massive error
                            // or simulate a crash by just throwing.
                            // Setting a timeout to throw outside the click handler often bypasses React's immediate catch
                            // if we want window.onerror to see it clearly, or we rely on Winnow's wrapping.
                            setTimeout(() => { throw new Error("CRITICAL_UI_FAILURE: Component undefined") }, 0);
                        }}
                        className="flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105"
                    >
                        <div className="mb-4 rounded-full bg-orange-100 dark:bg-orange-900/20 p-3">
                            <XCircle className="h-6 w-6 text-orange-600" />
                        </div>
                        <h3 className="font-bold">Crash App</h3>
                        <p className="text-sm text-muted-foreground mt-2">Throws Uncaught Exception</p>
                    </button>

                    <button
                        onClick={triggerConsoleError}
                        className="flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105"
                    >
                        <div className="mb-4 rounded-full bg-yellow-100 dark:bg-yellow-900/20 p-3">
                            <AlertTriangle className="h-6 w-6 text-yellow-600" />
                        </div>
                        <h3 className="font-bold">Console Spam</h3>
                        <p className="text-sm text-muted-foreground mt-2">Logs fake API errors</p>
                    </button>
                </div>
            </div>
        </section>
    );
}
