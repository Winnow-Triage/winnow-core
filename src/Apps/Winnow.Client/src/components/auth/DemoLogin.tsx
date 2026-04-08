import { Button } from "@/components/ui/button";

interface DemoLoginProps {
  isLoading: boolean;
  onLogin: () => void;
}

export function DemoLogin({ isLoading, onLogin }: DemoLoginProps) {
  return (
    <div className="mt-8 flex flex-col gap-3 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div className="relative">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t border-muted-foreground/20" />
        </div>
        <div className="relative flex justify-center text-[10px] uppercase tracking-widest text-muted-foreground">
          <span className="bg-background px-4 font-semibold">Demo Sandbox</span>
        </div>
      </div>
      <Button
        id="quick-demo-login"
        type="button"
        variant="secondary"
        className="w-full h-11 border-2 border-indigo-500/20 hover:border-indigo-500/50 hover:bg-indigo-500/5 text-indigo-600 dark:text-indigo-400 font-bold shadow-sm transition-all"
        onClick={onLogin}
        disabled={isLoading}
      >
        {isLoading ? "Entering Sandbox..." : "Quick Demo Login →"}
      </Button>
    </div>
  );
}
