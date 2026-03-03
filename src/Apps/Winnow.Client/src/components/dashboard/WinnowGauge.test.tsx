import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import WinnowGauge from "./WinnowGauge";

describe("WinnowGauge", () => {
  it("renders with default props", () => {
    render(<WinnowGauge percent={0.5} />);
    expect(screen.getByText("Winnow Ratio")).toBeInTheDocument();
    expect(screen.getByText("50%")).toBeInTheDocument();
  });

  it("renders with 0% percent", () => {
    render(<WinnowGauge percent={0} />);
    expect(screen.getByText("0%")).toBeInTheDocument();
  });

  it("renders with 100% percent", () => {
    render(<WinnowGauge percent={1} />);
    expect(screen.getByText("100%")).toBeInTheDocument();
  });

  it("renders with hoursSaved prop", () => {
    render(<WinnowGauge percent={0.75} hoursSaved={5} />);
    expect(screen.getByText("5 Hours Saved Today")).toBeInTheDocument();
  });

  it("does not render hoursSaved when not provided", () => {
    render(<WinnowGauge percent={0.75} />);
    const hoursSavedElement = screen.queryByText("Hours Saved Today");
    expect(hoursSavedElement).not.toBeInTheDocument();
  });
});
