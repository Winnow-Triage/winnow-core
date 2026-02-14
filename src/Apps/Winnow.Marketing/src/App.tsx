import { useEffect } from 'react';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Navbar } from './components/Navbar';
import { Footer } from './components/Footer';
import { Landing } from './pages/Landing';
import { Pricing } from './pages/Pricing';

// Declare Winnow on window object
declare global {
  interface Window {
    Winnow: {
      init: (config: any) => void;
    };
  }
}

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
          apiKey: 'secret-key', // Correct key provided by user
          tenantId: 'marketing-demo-tenant', // Using a specific tenant for marketing demo if possible, or default
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
      <Navbar />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/pricing" element={<Pricing />} />
        </Routes>
      </BrowserRouter>
      <Footer />
    </div>
  )
}

export default App
