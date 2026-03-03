import * as React from "react";
import { cn } from "@/lib/utils";

interface PageTitleProps extends React.HTMLAttributes<HTMLHeadingElement> {
  children: React.ReactNode;
}

const PageTitle = React.forwardRef<HTMLHeadingElement, PageTitleProps>(
  ({ className, children, ...props }, ref) => {
    return (
      <h1
        ref={ref}
        className={cn(
          "text-4xl md:text-5xl font-black tracking-tighter bg-clip-text text-transparent bg-gradient-to-br from-foreground to-foreground/60 leading-tight pb-2 px-2",
          className,
        )}
        {...props}
      >
        {children}
      </h1>
    );
  },
);
PageTitle.displayName = "PageTitle";

export { PageTitle };
