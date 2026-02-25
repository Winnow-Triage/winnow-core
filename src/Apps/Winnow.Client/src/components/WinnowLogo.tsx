// WinnowLogo component

interface WinnowLogoProps {
    className?: string;
    size?: number;
    showText?: boolean;
}

export function WinnowLogo({ className = "", size = 32, showText = true }: WinnowLogoProps) {
    return (
        <div className={`flex items-center gap-2 ${className}`}>
            <svg
                width={size}
                height={size}
                viewBox="0 0 100 100"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
                className="drop-shadow-sm shrink-0"
            >
                <defs>
                    <linearGradient id="logo-gradient-client" x1="0%" y1="0%" x2="100%" y2="100%">
                        <stop offset="0%" stopColor="#2563eb" /> {/* blue-600 */}
                        <stop offset="100%" stopColor="#9333ea" /> {/* purple-600 */}
                    </linearGradient>
                </defs>

                {/* Funnel 1 (Left) */}
                <path
                    d="M10 25 L35 75 L60 25"
                    stroke="url(#logo-gradient-client)"
                    strokeWidth="12"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity="0.9"
                />

                {/* Funnel 2 (Right) - Overlapping */}
                <path
                    d="M40 25 L65 75 L90 25"
                    stroke="url(#logo-gradient-client)"
                    strokeWidth="12"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    style={{ mixBlendMode: 'multiply' }}
                    className="dark:mix-blend-screen"
                />
            </svg>
            {showText && (
                <span className="font-bold tracking-tight bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                    Winnow
                </span>
            )}
        </div>
    );
}
