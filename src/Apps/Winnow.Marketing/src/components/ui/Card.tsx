import type { ReactNode } from 'react';

interface CardProps {
    children: ReactNode;
    className?: string;
    isHoverable?: boolean;
    variant?: 'default' | 'primary' | 'dark';
}

export function Card({
    children,
    className = "",
    isHoverable = true,
    variant = 'default'
}: CardProps) {
    const baseStyles = "flex flex-col p-8 rounded-3xl border transition-all duration-300";

    const variants = {
        default: "bg-white dark:bg-slate-950 text-slate-900 dark:text-slate-50 border-slate-200 dark:border-white/5 shadow-sm",
        primary: "bg-white dark:bg-slate-950 border-primary shadow-2xl scale-105 z-10",
        dark: "bg-slate-900 dark:bg-slate-900 text-white border-slate-800 shadow-sm"
    };

    const hoverStyles = isHoverable
        ? "hover:shadow-xl hover:-translate-y-1"
        : "";

    return (
        <div className={`${baseStyles} ${variants[variant]} ${hoverStyles} ${className}`}>
            {children}
        </div>
    );
}
