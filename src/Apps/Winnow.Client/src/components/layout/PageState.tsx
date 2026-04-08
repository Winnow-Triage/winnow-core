import React from "react";
import { Loader2, AlertCircle } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { PageTitle } from "@/components/ui/page-title";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/**
 * PageState - A wrapper for main page content that provides standard entry animations.
 */
export function PageState({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("animate-in fade-in duration-700 h-full", className)}>
      {children}
    </div>
  );
}

/**
 * LoadingState - A standard centered spinner for page transitions.
 */
export function LoadingState({ message = "Loading..." }: { message?: string }) {
  return (
    <div className="flex h-[60vh] w-full items-center justify-center animate-in fade-in duration-500">
      <div className="flex flex-col items-center gap-3">
        <Loader2 className="h-10 w-10 animate-spin text-primary opacity-80" />
        <p className="text-sm font-medium text-muted-foreground animate-pulse">{message}</p>
      </div>
    </div>
  );
}

/**
 * ErrorState - A unified error display component.
 */
export function ErrorState({ 
  title = "Error", 
  message, 
  onRetry 
}: { 
  title?: string;
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="p-8 w-full animate-in zoom-in-95 duration-300">
      <div className="max-w-2xl mx-auto bg-destructive/10 text-destructive p-6 rounded-xl border border-destructive/20 flex flex-col md:flex-row gap-4 items-center md:items-start text-center md:text-left">
        <div className="bg-destructive/10 p-2 rounded-full">
          <AlertCircle className="h-6 w-6" />
        </div>
        <div className="flex-1 space-y-2">
          <p className="font-bold text-lg">{title}</p>
          <p className="text-sm opacity-90 leading-relaxed">
            {message}
          </p>
          {onRetry && (
            <div className="pt-2">
              <Button 
                variant="destructive" 
                size="sm" 
                onClick={onRetry}
                className="font-semibold"
              >
                Try Again
              </Button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/**
 * EmptyState - Configurable view for "No results" or "Nothing here".
 */
export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}) {
  return (
    <div className="flex flex-col items-center justify-center h-64 w-full text-center p-8 animate-in fade-in slide-in-from-bottom-2 duration-500">
      <div className="bg-muted p-4 rounded-full mb-4">
        <Icon className="h-10 w-10 text-muted-foreground opacity-60" />
      </div>
      <h3 className="font-bold text-xl tracking-tight">{title}</h3>
      <p className="text-muted-foreground mt-2 max-w-sm text-sm">
        {description}
      </p>
      {action && (
        <Button variant="outline" size="sm" onClick={action.onClick} className="mt-6 hover:bg-secondary">
          {action.label}
        </Button>
      )}
    </div>
  );
}

/**
 * PageHeader - Standard layout for PageTitle + Description.
 */
export function PageHeader({ 
  title, 
  description, 
  children 
}: { 
  title: string; 
  description?: string;
  children?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-2">
      <div className="flex flex-col gap-1.5">
        <PageTitle>{title}</PageTitle>
        {description && (
          <p className="text-muted-foreground text-[0.95rem] max-w-2xl leading-relaxed">
            {description}
          </p>
        )}
      </div>
      {children && <div className="flex items-center gap-3">{children}</div>}
    </div>
  );
}
