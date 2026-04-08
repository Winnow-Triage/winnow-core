import { CheckCircle2, Circle } from "lucide-react";
import { cn } from "@/lib/utils";
import { passwordRules } from "@/lib/auth-utils";

interface PasswordRulesProps {
  password: string;
  className?: string;
}

export function PasswordRules({ password, className }: PasswordRulesProps) {
  return (
    <div
      className={cn(
        "space-y-2 p-3 bg-muted/50 rounded-lg border border-border/50 text-sm",
        className,
      )}
    >
      <p className="text-xs font-semibold text-muted-foreground uppercase tracking-tight mb-1">
        Password Requirements
      </p>
      <div className="grid grid-cols-1 gap-1.5">
        {passwordRules.map((rule, i) => {
          const isValid = rule.test(password);
          return (
            <div
              key={i}
              className={cn(
                "flex items-center gap-2 transition-colors duration-200",
                isValid ? "text-primary font-medium" : "text-muted-foreground",
              )}
            >
              {isValid ? (
                <CheckCircle2 className="h-3.5 w-3.5" />
              ) : (
                <Circle className="h-3.5 w-3.5" />
              )}
              <span className="text-xs">{rule.label}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
