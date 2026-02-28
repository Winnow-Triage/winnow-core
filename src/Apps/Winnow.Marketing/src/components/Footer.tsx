import { Link } from 'react-router-dom';
import { WinnowLogo } from './WinnowLogo';
import { FaDiscord, FaLinkedin } from 'react-icons/fa';
import { FaXTwitter } from 'react-icons/fa6';
import { SiBluesky } from 'react-icons/si';

export function Footer() {
    return (
        <footer className="bg-slate-950 py-12 md:py-16 text-slate-400">
            <div className="container mx-auto px-4 md:px-6">
                <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
                    {/* Brand Column */}
                    <div className="col-span-2 md:col-span-1">
                        <Link to="/" className="flex items-center space-x-2 font-bold text-white text-xl mb-4">
                            <WinnowLogo size={32} />
                        </Link>
                        <p className="text-sm leading-relaxed max-w-xs mb-6">
                            Triage at the speed of AI. Stop drowning in duplicate bug reports and start fixing what matters.
                        </p>
                        <div className="flex items-center space-x-4">
                            <a href="https://discord.gg/winnow" target="_blank" rel="noreferrer" className="hover:text-white transition-colors" title="Discord">
                                <FaDiscord className="h-5 w-5" />
                            </a>
                            <a href="https://x.com/winnowtriage" target="_blank" rel="noreferrer" className="hover:text-white transition-colors" title="Twitter / X">
                                <FaXTwitter className="h-5 w-5" />
                            </a>
                            <a href="https://bsky.app/profile/winnowtriage.bsky.social" target="_blank" rel="noreferrer" className="hover:text-white transition-colors" title="Bluesky">
                                <SiBluesky className="h-5 w-5" />
                            </a>
                            <a href="https://linkedin.com/company/winnow-triage" target="_blank" rel="noreferrer" className="hover:text-white transition-colors" title="LinkedIn">
                                <FaLinkedin className="h-5 w-5" />
                            </a>
                        </div>
                    </div>

                    {/* Product Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Product</h4>
                        <ul className="space-y-3 text-sm">
                            <li><Link to="/features" className="hover:text-white transition-colors">Features</Link></li>
                            <li><Link to="/integrations" className="hover:text-white transition-colors">Integrations</Link></li>
                            <li><Link to="/docs" className="hover:text-white transition-colors">SDK Documentation</Link></li>
                            <li><Link to="/pricing" className="hover:text-white transition-colors">Pricing</Link></li>
                        </ul>
                    </div>

                    {/* Company Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Company</h4>
                        <ul className="space-y-3 text-sm">
                            <li><Link to="/about" className="hover:text-white transition-colors">About Us</Link></li>
                            <li><Link to="/blog" className="hover:text-white transition-colors">Blog</Link></li>
                            <li><Link to="/contact" className="hover:text-white transition-colors">Contact</Link></li>
                        </ul>
                    </div>

                    {/* Legal Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Legal</h4>
                        <ul className="space-y-3 text-sm">
                            <li><Link to="/privacy" className="hover:text-white transition-colors">Privacy Policy</Link></li>
                            <li><Link to="/terms" className="hover:text-white transition-colors">Terms of Service</Link></li>
                            <li><Link to="/cookies" className="hover:text-white transition-colors">Cookie Policy</Link></li>
                        </ul>
                    </div>
                </div>

                <div className="pt-8 border-t border-slate-800 flex flex-col md:flex-row justify-between items-center gap-4 text-sm">
                    <p>© 2026 Winnow Triage, LLC. All rights reserved.</p>
                    <p>Built with ❤️ in Fort Worth.</p>
                </div>
            </div>
        </footer>
    );
}
