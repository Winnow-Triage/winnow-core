import { Info } from "lucide-react";

export function SocialAuth() {
  return (
    <div className="space-y-4 w-full">
      <div className="flex items-center justify-center p-4 rounded-lg bg-secondary/50 border border-dashed border-muted-foreground/30 group transition-all hover:bg-secondary/70">
        <div className="text-center space-y-1">
          <div className="flex items-center justify-center gap-2 text-muted-foreground group-hover:text-primary transition-colors">
            <Info className="h-4 w-4" />
            <span className="text-xs font-semibold uppercase tracking-widest">External Auth Coming Soon</span>
          </div>
          <p className="text-[10px] text-muted-foreground/70">
            GitHub, Google, and Okta SSO integration is currently in development.
          </p>
        </div>
      </div>

      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-background px-2 text-muted-foreground">
            Sign in with email
          </span>
        </div>
      </div>
    </div>
  );
}
