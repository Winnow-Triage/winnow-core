import { WinnowLogo } from "@/components/WinnowLogo";
import { QuoteRotator } from "./QuoteRotator";

export function AuthBrandingPanel() {
  return (
    <div className="hidden lg:flex w-[40%] bg-gradient-to-br from-slate-900 via-blue-950 to-purple-950 flex-col justify-center p-12 relative overflow-hidden">
      {/* Background Pattern/Gradient Overlay */}
      <div className="absolute inset-0 bg-blue-500/10 mix-blend-overlay" />
      <div className="absolute top-0 right-0 w-96 h-96 bg-blue-500/20 rounded-full blur-3xl -translate-y-1/2 translate-x-1/2 opacity-50" />
      <div className="absolute bottom-0 left-0 w-96 h-96 bg-purple-500/20 rounded-full blur-3xl translate-y-1/2 -translate-x-1/2 opacity-50" />

      {/* Logo - Top Left */}
      <div className="absolute top-8 left-8 z-20">
        <WinnowLogo size={32} className="text-white" />
      </div>

      {/* Rotating Quote - Centered */}
      <div className="relative z-10 px-8 flex flex-col items-start min-h-[160px]">
        <QuoteRotator />
      </div>

      {/* Attribution/Footer - Bottom Left */}
      <div className="absolute bottom-8 left-8 z-10">
        <p className="text-sm font-medium text-blue-200/60 uppercase tracking-wider">
          Trusted by developers who value their sanity.
        </p>
      </div>
    </div>
  );
}
