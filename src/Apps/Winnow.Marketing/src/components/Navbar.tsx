import { Moon, Sun } from 'lucide-react';
import { useEffect, useState } from 'react';

export function Navbar() {
    const [isDark, setIsDark] = useState(false);

    useEffect(() => {
        // Check system preference or localStorage
        if (localStorage.theme === 'dark' || (!('theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
            setIsDark(true);
            document.documentElement.classList.add('dark');
        } else {
            setIsDark(false);
            document.documentElement.classList.remove('dark');
        }
    }, []);

    const toggleTheme = () => {
        if (isDark) {
            document.documentElement.classList.remove('dark');
            localStorage.theme = 'light';
            setIsDark(false);
        } else {
            document.documentElement.classList.add('dark');
            localStorage.theme = 'dark';
            setIsDark(true);
        }
    };

    return (
        <nav className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
            <div className="container mx-auto flex h-14 items-center">
                <div className="mr-4 flex">
                    <a className="mr-6 flex items-center space-x-2 font-bold" href="/">
                        Winnow
                    </a>
                </div>
                <div className="flex flex-1 items-center justify-between space-x-2 md:justify-end">
                    <div className="w-full flex-1 md:w-auto md:flex-none">
                        {/* Placeholder for search/other nav items if needed */}
                    </div>
                    <nav className="flex items-center space-x-4">
                        <a
                            href="/docs"
                            className="text-sm font-medium text-muted-foreground transition-colors hover:text-primary"
                        >
                            Documentation
                        </a>
                        <button
                            onClick={toggleTheme}
                            className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-input bg-transparent shadow-sm hover:bg-accent hover:text-accent-foreground"
                        >
                            <span className="sr-only">Toggle theme</span>
                            {isDark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
                        </button>
                        <a
                            href="http://localhost:5173" // Link to main app
                            className="inline-flex h-9 items-center justify-center rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground shadow transition-colors hover:bg-primary/90 focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:pointer-events-none disabled:opacity-50"
                        >
                            Login
                        </a>
                    </nav>
                </div>
            </div>
        </nav>
    );
}
