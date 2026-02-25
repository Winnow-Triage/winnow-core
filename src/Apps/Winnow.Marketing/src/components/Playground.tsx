import { useState, useEffect } from 'react';
import { Bug, XCircle, AlertTriangle, Terminal } from 'lucide-react';

export function Playground() {
    const [counts, setCounts] = useState({ 404: 0, crash: 0, spam: 0 });
    const [lastAction, setLastAction] = useState<string | null>(null);

    // Clear last action message after a few seconds
    useEffect(() => {
        if (lastAction) {
            const timer = setTimeout(() => setLastAction(null), 3000);
            return () => clearTimeout(timer);
        }
    }, [lastAction]);

    const trigger404 = async () => {
        setCounts(prev => ({ ...prev, 404: prev[404] + 1 }));
        setLastAction("Triggered 404: /api/ghost-endpoint fetch failed");
        try {
            await fetch('/api/ghost-endpoint');
        } catch (e) {
            console.error(e);
        }
    };

    const triggerConsoleError = () => {
        setCounts(prev => ({ ...prev, spam: prev.spam + 1 }));
        setLastAction("Console Spam: Logged 504 Gateway Timeout");
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
                        className="group flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105 relative overflow-hidden"
                    >
                        {counts[404] > 0 && (
                            <span className="absolute top-2 right-2 bg-red-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full animate-in zoom-in">
                                {counts[404]}
                            </span>
                        )}
                        <div className="mb-4 rounded-full bg-red-100 dark:bg-red-900/20 p-3 group-hover:scale-110 transition-transform">
                            <Bug className="h-6 w-6 text-red-600" />
                        </div>
                        <h3 className="font-bold">Trigger 404</h3>
                        <p className="text-sm text-muted-foreground mt-2">Fails a fetch request</p>
                    </button>

                    <button
                        onClick={() => {
                            setCounts(prev => ({ ...prev, crash: prev.crash + 1 }));
                            setLastAction("Crash App: Uncaught Exception thrown");
                            setTimeout(() => { throw new Error("CRITICAL_UI_FAILURE: Component undefined") }, 0);
                        }}
                        className="group flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105 relative overflow-hidden"
                    >
                        {counts.crash > 0 && (
                            <span className="absolute top-2 right-2 bg-orange-500 text-white text-[10px] font-bold px-1.5 py-0.5 rounded-full animate-in zoom-in">
                                {counts.crash}
                            </span>
                        )}
                        <div className="mb-4 rounded-full bg-orange-100 dark:bg-orange-900/20 p-3 group-hover:scale-110 transition-transform">
                            <XCircle className="h-6 w-6 text-orange-600" />
                        </div>
                        <h3 className="font-bold">Crash App</h3>
                        <p className="text-sm text-muted-foreground mt-2">Throws Uncaught Exception</p>
                    </button>

                    <button
                        onClick={triggerConsoleError}
                        className="group flex flex-col items-center justify-center rounded-lg border bg-background p-8 hover:bg-accent hover:text-accent-foreground transition-all hover:scale-105 relative overflow-hidden"
                    >
                        {counts.spam > 0 && (
                            <span className="absolute top-2 right-2 bg-yellow-500 text-black text-[10px] font-bold px-1.5 py-0.5 rounded-full animate-in zoom-in">
                                {counts.spam}
                            </span>
                        )}
                        <div className="mb-4 rounded-full bg-yellow-100 dark:bg-yellow-900/20 p-3 group-hover:scale-110 transition-transform">
                            <AlertTriangle className="h-6 w-6 text-yellow-600" />
                        </div>
                        <h3 className="font-bold">Console Spam</h3>
                        <p className="text-sm text-muted-foreground mt-2">Logs fake API errors</p>
                    </button>
                </div>

                <div className="mt-6 min-h-12 flex items-center justify-center">
                    {lastAction && (
                        <div className="flex items-center gap-2 px-4 py-2 rounded-full bg-slate-900/50 border border-slate-700 text-slate-300 text-sm animate-in fade-in slide-in-from-bottom-2">
                            <Terminal className="h-4 w-4 text-emerald-400" />
                            <span className="font-mono">{lastAction}</span>
                        </div>
                    )}
                </div>
            </div>
        </section>
    );
}
