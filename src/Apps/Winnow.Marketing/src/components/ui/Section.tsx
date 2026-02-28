import type { ReactNode } from 'react';

interface SectionProps {
    children: ReactNode;
    className?: string;
    id?: string;
    containerClassName?: string;
    variant?: 'white' | 'slate' | 'muted' | 'transparent';
    border?: 'top' | 'bottom' | 'both' | 'none';
    padding?: 'normal' | 'large' | 'xlarge' | 'none';
}

export function Section({
    children,
    className = "",
    id,
    containerClassName = "",
    variant = 'transparent',
    border = 'none',
    padding = 'normal'
}: SectionProps) {
    const variants = {
        white: "bg-white text-slate-900 border-slate-200",
        slate: "bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-white transition-colors duration-300",
        muted: "bg-slate-50 dark:bg-slate-900/50",
        transparent: "bg-transparent"
    };

    const borders = {
        top: "border-t",
        bottom: "border-b",
        both: "border-y",
        none: ""
    };

    const paddings = {
        normal: "py-20",
        large: "py-24 md:py-32",
        xlarge: "py-32 md:py-48",
        none: ""
    };

    return (
        <section
            id={id}
            className={`relative overflow-hidden ${variants[variant]} ${borders[border]} ${paddings[padding]} ${className}`}
        >
            <div className={`container mx-auto px-4 md:px-6 relative z-10 ${containerClassName}`}>
                {children}
            </div>
        </section>
    );
}
