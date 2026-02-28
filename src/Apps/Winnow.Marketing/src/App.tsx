import { useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Navbar } from './components/Navbar';
import { Footer } from './components/Footer';
import { Landing } from './pages/Landing';
import { Pricing } from './pages/Pricing';
import { Features } from './pages/Features';
import { About } from './pages/About';
import { Contact } from './pages/Contact';
import { Privacy } from './pages/Privacy';
import { Terms } from './pages/Terms';
import { Integrations } from './pages/Integrations';

const ComingSoon = ({ title }: { title: string }) => (
  <div className="py-32 text-center bg-slate-50 dark:bg-slate-900/50">
    <h1 className="text-4xl font-bold mb-4">{title}</h1>
    <p className="text-xl text-muted-foreground">We're working hard to bring you this content. Stay tuned!</p>
  </div>
);

// Declare Winnow on window object
declare global {
  interface Window {
    Winnow: {
      init: (config: any) => void;
    };
  }
}

import { ScrollToTop } from './components/ScrollToTop';

function App() {
  useEffect(() => {
    // Dynamically load the Winnow SDK from the local server
    // In production, this would be a CDN link
    const script = document.createElement('script');
    script.src = "http://localhost:5294/sdk/winnow.iife.js";
    script.async = true;
    script.onload = () => {
      // Initialize Winnow once the script captures
      if (window.Winnow) {
        window.Winnow.init({
          apiKey: 'wm_live_058cba8de3cb46d1b813d85c6a9be2a9_nQ7DBN1fH0Tkr9J3FPtF3D1OtxCa8Pq9LHd3EPn-8eo', // Correct key provided by user
          apiUrl: 'http://localhost:5294', // Pointing to local backend
          debug: true
        });
        console.log("Winnow SDK Initialized on Marketing Site");
      }
    };
    document.body.appendChild(script);

    return () => {
      // Cleanup if needed, though usually SDKs persist
    }
  }, []);

  return (
    <div className="min-h-screen bg-background font-sans antialiased">
      <BrowserRouter>
        <ScrollToTop />
        <Navbar />
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/features" element={<Features />} />
          <Route path="/pricing" element={<Pricing />} />
          <Route path="/about" element={<About />} />
          <Route path="/contact" element={<Contact />} />
          <Route path="/privacy" element={<Privacy />} />
          <Route path="/terms" element={<Terms />} />
          <Route path="/integrations" element={<Integrations />} />
          <Route path="/blog" element={<ComingSoon title="Blog" />} />
          <Route path="/careers" element={<ComingSoon title="Careers" />} />
          <Route path="/cookies" element={<ComingSoon title="Cookie Policy" />} />
          <Route path="/docs" element={<ComingSoon title="SDK Documentation" />} />
        </Routes>
        <Footer />
      </BrowserRouter>
    </div>
  )
}

export default App
