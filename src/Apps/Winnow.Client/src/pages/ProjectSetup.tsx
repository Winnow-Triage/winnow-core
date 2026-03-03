import { useState, useEffect } from "react";
import { Check, Copy, Server, ArrowRight, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { api } from "@/lib/api";
import { useLocation } from "react-router-dom";

const codeSnippets = {
  js: {
    label: "JavaScript",
    code: `import { Winnow } from '@winnow/sdk';

Winnow.init({
  apiKey: "secret-key",
  release: "v1.0.0"
});`,
  },
  csharp: {
    label: ".NET Core",
    code: `using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

var config = new WinnowConfig { Environment = "Production" };
var winnow = new WinnowSdkClient("secret-key", config);`,
  },
  unity: {
    label: "Unity",
    code: `using UnityEngine;
using Winnow.Sdk.DotNet.Unity;

public class GameInit : MonoBehaviour
{
    void Start()
    {
        var winnow = gameObject.AddComponent<WinnowUnity>();
        winnow.apiKey = "secret-key";
    }
}`,
  },
  godot: {
    label: "Godot",
    code: `// Ensure WinnowGodot is in Project -> AutoLoad
using Godot;
using Winnow.Sdk.DotNet.GodotNode;

public override void _Ready()
{
    var winnow = new WinnowGodot { ApiKey = "secret-key" };
    AddChild(winnow);
}`,
  },
};

export default function ProjectSetup() {
  // State
  const [activeTab, setActiveTab] = useState<
    "js" | "csharp" | "unity" | "godot"
  >("js");
  const [keyCopied, setKeyCopied] = useState(false);
  const [snippetCopied, setSnippetCopied] = useState(false);
  const [connectionState, setConnectionState] = useState<"waiting" | "success">(
    "waiting",
  );
  const location = useLocation();
  // const navigate = useNavigate()

  const apiKey =
    location.state?.apiKey || "Click 'Regenerate API Key' in Settings";

  // Polling Logic
  useEffect(() => {
    if (connectionState === "success") return;

    const checkReports = async () => {
      try {
        const { data: reports } = await api.get("/reports");
        if (Array.isArray(reports) && reports.length > 0) {
          setConnectionState("success");
        }
      } catch (error) {
        console.error("Polling failed (server might be down):", error);
      }
    };

    // Initial check
    checkReports();

    // Poll every 3 seconds
    const interval = setInterval(checkReports, 3000);
    return () => clearInterval(interval);
  }, [connectionState]);

  const handleCopySnippet = () => {
    navigator.clipboard.writeText(codeSnippets[activeTab].code);
    setSnippetCopied(true);
    setTimeout(() => setSnippetCopied(false), 2000);
  };

  const handleCopyKey = () => {
    navigator.clipboard.writeText(apiKey);
    setKeyCopied(true);
    setTimeout(() => setKeyCopied(false), 2000);
  };

  return (
    <div className="min-h-[calc(100vh-4rem)] flex flex-col items-center justify-center p-4 md:p-8 animate-in fade-in duration-500">
      <div className="max-w-5xl w-full grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
        {/* Section A: Initialize Winnow (The IDE) */}
        <div className="flex flex-col space-y-6">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">
              Initialize Winnow.
            </h1>
            <p className="text-muted-foreground mt-2">
              Add the SDK to your project to start tracking errors.
            </p>
          </div>

          {/* API Key Box */}
          <Card className="p-4 bg-secondary/50 border-primary/20 flex items-center justify-between">
            <div className="font-mono text-sm truncate mr-4">
              <span className="text-muted-foreground select-none">
                API Key:{" "}
              </span>
              <span className="text-primary font-bold">{apiKey}</span>
            </div>
            <Button variant="ghost" size="sm" onClick={handleCopyKey}>
              {keyCopied ? (
                <Check className="h-4 w-4" />
              ) : (
                <Copy className="h-4 w-4" />
              )}
            </Button>
          </Card>

          {/* Floating IDE */}
          <div className="rounded-xl overflow-hidden shadow-2xl bg-[#0F172A] ring-1 ring-white/10 text-slate-300">
            {/* Tab Bar */}
            <div className="flex items-center justify-between px-2 bg-white/5 border-b border-white/10">
              <div className="flex space-x-1">
                {Object.entries(codeSnippets).map(([key, snippet]) => (
                  <button
                    key={key}
                    onClick={() => setActiveTab(key as any)}
                    className={cn(
                      "px-4 py-3 text-xs font-medium transition-all relative outline-none",
                      activeTab === key
                        ? "text-white bg-white/10"
                        : "text-slate-400 hover:text-white hover:bg-white/5",
                    )}
                  >
                    {snippet.label}
                    {activeTab === key && (
                      <div className="absolute top-0 left-0 right-0 h-0.5 bg-blue-500" />
                    )}
                  </button>
                ))}
              </div>
              <Button
                variant="ghost"
                size="icon"
                onClick={handleCopySnippet}
                className="h-8 w-8 text-slate-400 hover:text-white hover:bg-white/10"
              >
                {snippetCopied ? (
                  <Check className="h-4 w-4 text-green-400" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </div>

            {/* Code Area */}
            <div className="p-6 font-mono text-sm overflow-x-auto bg-[#0F172A] min-h-[200px]">
              <pre>{codeSnippets[activeTab].code}</pre>
            </div>
          </div>
        </div>

        {/* Section B: Connection Doctor */}
        <div className="flex flex-col space-y-6 lg:pl-12 lg:border-l border-border h-full justify-center">
          {connectionState === "waiting" && (
            <div className="flex flex-col items-center justify-center text-center space-y-6 py-12 animate-in zoom-in-95 duration-500">
              {/* Radar Animation */}
              <div className="relative">
                <div className="absolute inset-0 bg-blue-500/30 rounded-full animate-ping opacity-75" />
                <div className="relative bg-background border-2 border-blue-500 rounded-full p-6 shadow-[0_0_30px_-5px_hsl(var(--primary))]">
                  <Server className="h-10 w-10 text-blue-500 animate-pulse" />
                </div>
              </div>

              <div className="space-y-2">
                <h2 className="text-xl font-semibold">
                  Waiting for your first event...
                </h2>
                <p className="text-sm text-muted-foreground flex items-center justify-center gap-2">
                  <Loader2 className="h-3 w-3 animate-spin" />
                  Listening on port 5294...
                </p>
              </div>
            </div>
          )}

          {connectionState === "success" && (
            <div className="flex flex-col items-center justify-center text-center space-y-6 py-12 animate-in zoom-in-95 duration-500">
              {/* Success State */}
              <div className="relative">
                <div className="absolute inset-0 bg-green-500/20 rounded-full blur-xl" />
                <div className="relative bg-green-500 text-white rounded-full p-6 shadow-2xl scale-110">
                  <Check className="h-10 w-10 stroke-[3]" />
                </div>
              </div>

              <div className="space-y-2">
                <h2 className="text-2xl font-bold text-green-500">
                  Connection Established!
                </h2>
                <p className="text-muted-foreground">
                  We received your first event. You're ready to go.
                </p>
              </div>

              <div className="pt-4">
                <Button
                  size="lg"
                  className="bg-blue-600 hover:bg-blue-700 text-white shadow-lg shadow-blue-500/25 w-full sm:w-auto"
                  onClick={() => (window.location.href = "/dashboard")}
                >
                  Go to Dashboard <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
