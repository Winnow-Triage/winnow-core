import { useState, useCallback, useLayoutEffect } from "react";

export interface ChartDimensions {
  width: number;
  height: number;
}

export function useChartDimensions() {
  const [dimensions, setDimensions] = useState<ChartDimensions>({
    width: 0,
    height: 0,
  });
  const [element, setElement] = useState<HTMLDivElement | null>(null);

  // Callback ref ensures we observe the element even if it's conditionally rendered
  const ref = useCallback((node: HTMLDivElement | null) => {
    setElement(node);
  }, []);

  useLayoutEffect(() => {
    if (!element) return;

    const resizeObserver = new ResizeObserver((entries) => {
      if (!entries.length) return;

      // Get the size of the box
      const entry = entries[0];
      const { width, height } = entry.contentRect;

      // Only update if we have meaningful dimensions
      // Adding a small buffer as Recharts sometimes balks at very small numbers
      if (width > 0.5 && height > 0.5) {
        setDimensions({ width, height });
      }
    });

    resizeObserver.observe(element);

    return () => {
      resizeObserver.disconnect();
    };
  }, [element]);

  return { ref, dimensions };
}
