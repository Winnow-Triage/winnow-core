import type { ReactNode } from 'react';

interface GradientTextProps {
    children: ReactNode;
    className?: string;
}

export function GradientText({ children, className = "" }: GradientTextProps) {
    return (
        <span className={`text-brand-gradient italic ${className}`}>
            {children}
        </span>
    );
}
