import { ShieldAlert } from "lucide-react";

export default function DemoBanner() {
  return (
    <div className="bg-indigo-600/95 backdrop-blur-sm text-white h-7 px-4 text-[10px] uppercase tracking-wider font-bold flex items-center justify-center gap-2 sticky top-0 z-[100] border-b border-white/10 select-none shrink-0">
      <ShieldAlert className="h-3 w-3 text-indigo-200" />
      <span>Secure Demo Sandbox &bull; Data Not Persisted &bull; All Responses are Simulated</span>
    </div>
  );
}
