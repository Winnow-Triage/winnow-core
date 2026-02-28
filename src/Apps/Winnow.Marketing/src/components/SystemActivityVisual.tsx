import { useState, useEffect, useRef } from 'react';
import { Terminal, Shield, Search } from 'lucide-react';

interface Signal {
    id: number;
    x: number;
    y: number;
    type: 'error' | 'log';
    status: 'incoming' | 'processing' | 'clustered';
    startTime: number;
}

export function SystemActivityVisual() {
    const [signals, setSignals] = useState<Signal[]>([]);
    const [logs, setLogs] = useState<string[]>(["SYSTEM READY", "AWAITING SIGNALS..."]);
    const [stats, setStats] = useState({ health: 100, efficiency: 99.4 });
    const signalIdCounter = useRef(0);

    useEffect(() => {
        const interval = setInterval(() => {
            // Add new signals
            if (Math.random() > 0.6) {
                const newSignal: Signal = {
                    id: signalIdCounter.current++,
                    x: Math.random() * 80 + 10, // Anywhere along the top
                    y: 0, // Start from top
                    type: Math.random() > 0.3 ? 'error' : 'log',
                    status: 'incoming',
                    startTime: Date.now()
                };
                setSignals(prev => [...prev.slice(-20), newSignal]);

                const message = newSignal.type === 'error'
                    ? `INGESTING EXCEPTION: ${Math.random().toString(36).substring(7).toUpperCase()}`
                    : `LOG RECEIVED: ACCESS_CHECK_${Math.random().toString(10).substring(2, 5)}`;

                setLogs(prev => [message, ...prev.slice(0, 4)]);
            }

            // Update stats slightly
            setStats(prev => ({
                health: Math.min(100, Math.max(98, prev.health + (Math.random() - 0.5) * 0.1)),
                efficiency: Math.min(99.9, Math.max(99, prev.efficiency + (Math.random() - 0.5) * 0.05))
            }));
        }, 1500);

        return () => clearInterval(interval);
    }, []);

    // Animation frames for signals
    useEffect(() => {
        const animationInterval = setInterval(() => {
            setSignals(prev => prev.map(s => {
                if (s.status === 'incoming') {
                    const targetX = 50;
                    const targetY = 50;
                    const dx = (targetX - s.x) * 0.05;
                    const dy = (targetY - s.y) * 0.05;
                    const newX = s.x + dx;
                    const newY = s.y + dy;
                    if (Math.abs(newX - targetX) < 1 && Math.abs(newY - targetY) < 1) {
                        return { ...s, x: targetX, y: targetY, status: 'processing', startTime: Date.now() };
                    }
                    return { ...s, x: newX, y: newY };
                } else if (s.status === 'processing') {
                    const elapsed = Date.now() - s.startTime;
                    if (elapsed > 2000) {
                        return { ...s, status: 'clustered', startTime: Date.now() };
                    }
                } else if (s.status === 'clustered') {
                    // Target coordinates relative to the Cluster A/B boxes (top-45%)
                    const targetX = s.id % 2 === 0 ? 10 : 90;
                    const targetY = 48 + (s.id % 4) * 3;
                    const dx = (targetX - s.x) * 0.05;
                    const dy = (targetY - s.y) * 0.05;
                    return { ...s, x: s.x + dx, y: s.y + dy };
                }
                return s;
            }));
        }, 50);

        return () => clearInterval(animationInterval);
    }, []);

    return (
        <div className="relative aspect-video rounded-3xl overflow-hidden border shadow-2xl bg-slate-950 font-sans">
            {/* Grid Background */}
            <div
                className="absolute inset-0 opacity-20"
                style={{
                    backgroundImage: `radial-gradient(circle at 1px 1px, #3b82f6 1px, transparent 0)`,
                    backgroundSize: '40px 40px'
                }}
            />

            {/* Central Node / AI Core */}
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-32 h-32 flex items-center justify-center">
                <div className="absolute inset-0 bg-blue-600/20 rounded-full blur-2xl animate-pulse" />
                <div className="relative z-10 w-16 h-16 rounded-2xl bg-slate-900 border border-blue-500/50 flex items-center justify-center shadow-[0_0_30px_rgba(59,130,246,0.5)]">
                    <Shield className="h-8 w-8 text-blue-400 animate-pulse" />
                </div>
                {/* Orbital lines */}
                <div className="absolute w-full h-full border border-blue-500/20 rounded-full animate-[spin_10s_linear_infinite]" />
                <div className="absolute w-4/5 h-4/5 border border-purple-500/20 rounded-full animate-[spin_15s_linear_infinite_reverse]" />
            </div>

            {/* Signals */}
            {signals.map(s => (
                <div
                    key={s.id}
                    className="absolute transition-all duration-300 pointer-events-none"
                    style={{
                        left: `${s.x}%`,
                        top: `${s.y}%`,
                        transform: 'translate(-50%, -50%)',
                        opacity: s.status === 'clustered' ? 0.3 : 1
                    }}
                >
                    <div className={`relative flex items-center justify-center`}>
                        <div className={`absolute inset-0 blur-md ${s.type === 'error' ? 'bg-red-500' : 'bg-blue-400'} opacity-50 animate-ping`} />
                        <div className={`w-3 h-3 rounded-full ${s.type === 'error' ? 'bg-red-500 shadow-[0_0_10px_rgba(239,68,68,0.8)]' : 'bg-blue-400 shadow-[0_0_10px_rgba(96,165,250,0.8)]'}`} />
                        {s.status === 'processing' && (
                            <Search className="absolute -top-4 -right-4 h-3 w-3 text-white/50 animate-bounce" />
                        )}
                    </div>
                </div>
            ))}

            {/* Cluster Zones */}
            <div className="absolute top-[45%] left-4 w-28 h-20 border border-dashed border-white/20 rounded-xl flex items-center justify-center bg-white/5 backdrop-blur-sm">
                <span className="text-[10px] uppercase font-bold text-white/40 tracking-wider">Cluster A</span>
            </div>
            <div className="absolute top-[45%] right-4 w-28 h-20 border border-dashed border-white/20 rounded-xl flex items-center justify-center bg-white/5 backdrop-blur-sm">
                <span className="text-[10px] uppercase font-bold text-white/40 tracking-wider">Cluster B</span>
            </div>

            {/* System Status Metrics */}
            <div className="absolute top-6 left-6 right-6 flex justify-between items-start pointer-events-none">
                <div className="flex gap-4">
                    <div className="flex flex-col">
                        <span className="text-[10px] font-bold text-blue-400/80 uppercase tracking-widest">System Health</span>
                        <span className="text-xl font-mono text-white tracking-tighter">{stats.health.toFixed(1)}%</span>
                    </div>
                    <div className="flex flex-col">
                        <span className="text-[10px] font-bold text-purple-400/80 uppercase tracking-widest">Efficiency</span>
                        <span className="text-xl font-mono text-white tracking-tighter">{stats.efficiency.toFixed(1)}%</span>
                    </div>
                </div>
                <div className="px-3 py-1 rounded-full bg-emerald-500/10 border border-emerald-500/20 flex items-center gap-2">
                    <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                    <span className="text-[10px] font-bold text-emerald-400 uppercase tracking-widest">Live Engine</span>
                </div>
            </div>

            {/* Console Log - Centered and Narrower */}
            <div className="absolute bottom-4 left-1/2 -translate-x-1/2 w-2/3 max-w-[400px] p-3 bg-black/60 backdrop-blur-md rounded-xl border border-white/10 overflow-hidden shadow-2xl">
                <div className="flex items-center gap-2 mb-1 border-b border-white/5 pb-1">
                    <Terminal className="h-3 w-3 text-emerald-400" />
                    <span className="text-[9px] font-mono text-emerald-400/70 font-bold uppercase tracking-widest">Activity Feed</span>
                </div>
                <div className="h-16 overflow-hidden flex flex-col-reverse gap-1">
                    {logs.map((log, i) => (
                        <div key={i} className="text-[10px] font-mono text-white/50 whitespace-nowrap overflow-hidden text-ellipsis animate-in slide-in-from-bottom-1 leading-tight">
                            <span className="text-emerald-500/30 mr-2">[{new Date().toLocaleTimeString([], { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' })}]</span>
                            {log}
                        </div>
                    ))}
                </div>
            </div>

            {/* Scan Line Effect */}
            <div className="absolute inset-0 pointer-events-none overflow-hidden">
                <div className="w-full h-1/2 bg-gradient-to-b from-blue-500/5 to-transparent absolute -top-1/2 left-0 animate-[scan_4s_linear_infinite]" />
            </div>

            <style>{`
                @keyframes scan {
                    0% { top: -50%; }
                    100% { top: 100%; }
                }
            `}</style>
        </div>
    );
}
