import { Moon, Sun } from 'lucide-react';
import { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { WinnowLogo } from './WinnowLogo';

export function Navbar() {
    const [isDark, setIsDark] = useState(false);
    const location = useLocation();

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

    const navItems = [
        { name: 'Features', path: '/features', group: 'product' },
        { name: 'Integrations', path: '/integrations', group: 'product' },
        { name: 'Pricing', path: '/pricing', group: 'product' },
        { name: 'Blog', path: '/blog', group: 'resources' },
        { name: 'Docs', path: '/docs', group: 'resources' },
        { name: 'About', path: '/about', group: 'resources' },
    ];

    const NavLink = ({ item }: { item: typeof navItems[0] }) => {
        const isActive = location.pathname === item.path || (item.path === '/blog' && location.pathname.startsWith('/blog'));

        return (
            <Link
                to={item.path}
                className={`relative text-sm font-semibold transition-all duration-200 px-1 py-1 group ${isActive
                        ? "text-primary font-bold"
                        : "text-muted-foreground hover:text-foreground"
                    }`}
            >
                {item.name}
                <span className={`absolute -bottom-1 left-0 h-0.5 bg-brand-gradient transition-all duration-300 rounded-full ${isActive ? "w-full" : "w-0 group-hover:w-full opacity-60"
                    }`} />
            </Link>
        );
    };

    return (
        <nav className="sticky top-0 z-50 w-full border-b border-slate-200/50 dark:border-slate-800/50 bg-background/80 backdrop-blur-xl transition-all duration-300">
            <div className="container mx-auto flex h-16 items-center justify-between px-4 sm:px-6">
                <div className="flex items-center gap-10">
                    <Link className="flex items-center space-x-2 transition-transform hover:scale-[1.02]" to="/">
                        <WinnowLogo size={32} />
                    </Link>

                    {/* Desktop Navigation */}
                    <div className="hidden lg:flex items-center gap-8">
                        {/* Product Group */}
                        <div className="flex items-center gap-6">
                            {navItems.filter(i => i.group === 'product').map(item => (
                                <NavLink key={item.path} item={item} />
                            ))}
                        </div>

                        <div className="h-4 w-px bg-slate-200 dark:bg-slate-800" />

                        {/* Resources Group */}
                        <div className="flex items-center gap-6">
                            {navItems.filter(i => i.group === 'resources').map(item => (
                                <NavLink key={item.path} item={item} />
                            ))}
                        </div>
                    </div>
                </div>

                <div className="flex items-center gap-4">
                    <button
                        onClick={toggleTheme}
                        className="inline-flex h-9 w-9 items-center justify-center rounded-xl border border-slate-200 dark:border-slate-800 bg-background/50 hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors shadow-sm"
                    >
                        <span className="sr-only">Toggle theme</span>
                        {isDark ? <Sun className="h-4 w-4 text-amber-500" /> : <Moon className="h-4 w-4 text-indigo-500" />}
                    </button>

                    <a
                        href="http://localhost:5173"
                        className="hidden sm:inline-flex h-10 items-center justify-center rounded-xl bg-primary px-6 text-sm font-bold text-primary-foreground shadow-lg shadow-primary/20 transition-all hover:bg-primary/90 hover:scale-[1.02] active:scale-[0.98]"
                    >
                        Login
                    </a>

                    {/* Mobile Menu Button Placeholder */}
                    <button className="lg:hidden p-2 text-muted-foreground">
                        <svg className="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                        </svg>
                    </button>
                </div>
            </div>
        </nav>
    );
}
