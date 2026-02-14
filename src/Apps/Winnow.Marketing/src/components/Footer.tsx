export function Footer() {
    return (
        <footer className="bg-slate-950 py-12 md:py-16 text-slate-400">
            <div className="container mx-auto px-4 md:px-6">
                <div className="grid grid-cols-2 md:grid-cols-4 gap-8 mb-12">
                    {/* Brand Column */}
                    <div className="col-span-2 md:col-span-1">
                        <a href="/" className="flex items-center space-x-2 font-bold text-white text-xl mb-4">
                            Winnow
                        </a>
                        <p className="text-sm leading-relaxed max-w-xs">
                            Triage at the speed of AI. Stop drowning in duplicate bug reports and start fixing what matters.
                        </p>
                    </div>

                    {/* Product Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Product</h4>
                        <ul className="space-y-3 text-sm">
                            <li><a href="#" className="hover:text-white transition-colors">Features</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Integrations</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">SDK Documentation</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Pricing</a></li>
                        </ul>
                    </div>

                    {/* Company Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Company</h4>
                        <ul className="space-y-3 text-sm">
                            <li><a href="#" className="hover:text-white transition-colors">About Us</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Blog</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Careers</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Contact</a></li>
                        </ul>
                    </div>

                    {/* Legal Column */}
                    <div>
                        <h4 className="font-semibold text-slate-100 mb-4">Legal</h4>
                        <ul className="space-y-3 text-sm">
                            <li><a href="#" className="hover:text-white transition-colors">Privacy Policy</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Terms of Service</a></li>
                            <li><a href="#" className="hover:text-white transition-colors">Cookie Policy</a></li>
                        </ul>
                    </div>
                </div>

                <div className="pt-8 border-t border-slate-800 flex flex-col md:flex-row justify-between items-center gap-4 text-sm">
                    <p>© 2026 Winnow Inc. All rights reserved.</p>
                    <p>Built with ❤️ in Fort Worth.</p>
                </div>
            </div>
        </footer>
    );
}
