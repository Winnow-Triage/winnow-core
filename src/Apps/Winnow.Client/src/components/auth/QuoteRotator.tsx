import { useState, useEffect } from "react";

export function QuoteRotator() {
  const quotes = [
    "Debug faster, sleep more.",
    "Triage at the speed of AI.",
    "Stop drowning in logs.",
    "From chaos to clarity.",
  ];
  const [index, setIndex] = useState(0);
  const [isVisible, setIsVisible] = useState(true);

  useEffect(() => {
    const interval = setInterval(() => {
      setIsVisible(false); // Trigger exit
      setTimeout(() => {
        setIndex((prev) => (prev + 1) % quotes.length);
        setIsVisible(true); // Trigger enter
      }, 500); // Wait for exit transition
    }, 4000);

    return () => clearInterval(interval);
  }, [quotes.length]);

  return (
    <div className="h-32 flex items-center">
      <h2
        className={`text-5xl font-extrabold tracking-tight lg:text-6xl text-white transition-all duration-500 ease-in-out transform
                ${isVisible ? "opacity-100 translate-y-0 blur-0" : "opacity-0 -translate-y-4 blur-sm"}`}
        style={{ textShadow: "0 4px 20px rgba(0,0,0,0.3)" }}
      >
        "{quotes[index]}"
      </h2>
    </div>
  );
}
