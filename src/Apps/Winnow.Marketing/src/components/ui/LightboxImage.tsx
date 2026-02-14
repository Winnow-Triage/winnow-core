import { useState, useEffect } from 'react';
import { X, ZoomIn } from 'lucide-react';
import { createPortal } from 'react-dom';

interface LightboxImageProps {
    src: string;
    alt: string;
    className?: string; // Applied to the wrapper div
    imageClassName?: string; // Applied directly to the img element (if needed for specific object-fit, etc.)
}

export function LightboxImage({ src, alt, className = "", imageClassName = "" }: LightboxImageProps) {
    const [isOpen, setIsOpen] = useState(false);

    // Disable body scroll when modal is open
    useEffect(() => {
        if (isOpen) {
            document.body.style.overflow = 'hidden';
        } else {
            document.body.style.overflow = 'unset';
        }
        return () => {
            document.body.style.overflow = 'unset';
        };
    }, [isOpen]);

    // Handle ESC key to close
    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') setIsOpen(false);
        };
        if (isOpen) {
            window.addEventListener('keydown', handleKeyDown);
        }
        return () => {
            window.removeEventListener('keydown', handleKeyDown);
        };
    }, [isOpen]);

    return (
        <>
            <div
                className={`relative group cursor-zoom-in overflow-hidden ${className}`}
                onClick={() => setIsOpen(true)}
                role="button"
                aria-label={`View full size image of ${alt}`}
                tabIndex={0}
                onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') setIsOpen(true); }}
            >
                <img
                    src={src}
                    alt={alt}
                    className={`w-full h-auto transition-transform duration-300 group-hover:scale-[1.02] ${imageClassName}`}
                />

                {/* Checkered background for transparency (visible if image has transparency) - Optional, but sticking to requested Hint Icon */}

                {/* Hint Icon */}
                <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity duration-300 bg-black/10 backdrop-blur-[1px]">
                    <div className="bg-white/90 dark:bg-slate-900/90 p-3 rounded-full shadow-lg transform translate-y-4 group-hover:translate-y-0 transition-all duration-300">
                        <ZoomIn className="w-6 h-6 text-primary" />
                    </div>
                </div>
            </div>

            {/* Portal the modal to body to ensure z-index works correctly */}
            {isOpen && createPortal(
                <div
                    className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/80 backdrop-blur-sm animate-in fade-in duration-200"
                    onClick={() => setIsOpen(false)}
                >
                    {/* Close Button */}
                    <button
                        className="absolute top-4 right-4 md:top-8 md:right-8 text-white/70 hover:text-white bg-black/50 hover:bg-black/70 p-2 rounded-full transition-colors z-[110]"
                        onClick={(e) => {
                            e.stopPropagation();
                            setIsOpen(false);
                        }}
                    >
                        <X className="w-8 h-8" />
                    </button>

                    {/* Image */}
                    <img
                        src={src}
                        alt={alt}
                        className="max-w-[95vw] max-h-[90vh] object-contain shadow-2xl rounded-lg animate-in zoom-in-95 duration-300"
                        onClick={(e) => e.stopPropagation()}
                    />
                </div>,
                document.body
            )}
        </>
    );
}
