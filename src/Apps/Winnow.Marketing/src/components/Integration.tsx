import { useState } from 'react';
import { Copy, Check } from 'lucide-react';

const codeSnippets = {
    js: {
        label: 'JavaScript',
        code: `import { Winnow } from '@winnow/sdk';

Winnow.init({
  apiKey: "wm_live_...",
  release: "v1.0.0"
});`,
        html: `<span class="text-purple-400">import</span> { <span class="text-yellow-300">Winnow</span> } <span class="text-purple-400">from</span> <span class="text-green-400">'@winnow/sdk'</span>;

<span class="text-yellow-300">Winnow</span>.<span class="text-blue-400">init</span>({
  <span class="text-sky-300">apiKey</span>: <span class="text-green-400">"wm_live_..."</span>,
  <span class="text-sky-300">release</span>: <span class="text-green-400">"v1.0.0"</span>
});`
    },
    react: {
        label: 'React',
        code: `import { WinnowProvider } from '@winnow/react';

export default function App() {
  return (
    <WinnowProvider apiKey="wm_live_...">
      <YourApp />
    </WinnowProvider>
  );
}`,
        html: `<span class="text-purple-400">import</span> { <span class="text-yellow-300">WinnowProvider</span> } <span class="text-purple-400">from</span> <span class="text-green-400">'@winnow/react'</span>;

<span class="text-purple-400">export default function</span> <span class="text-blue-400">App</span>() {
  <span class="text-purple-400">return</span> (
    &lt;<span class="text-yellow-300">WinnowProvider</span> <span class="text-sky-300">apiKey</span>=<span class="text-green-400">"wm_live_..."</span>&gt;
      &lt;<span class="text-yellow-300">YourApp</span> /&gt;
    &lt;/<span class="text-yellow-300">WinnowProvider</span>&gt;
  );
}`
    },
    csharp: {
        label: '.NET Core',
        code: `using Winnow.Sdk.DotNet.Core;
using Winnow.Sdk.DotNet.Core.Models;

var config = new WinnowConfig { Environment = "Production" };
var winnow = new WinnowSdkClient("wm_live_...", config);`,
        html: `<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Sdk.DotNet.Core</span>;
<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Sdk.DotNet.Core.Models</span>;

<span class="text-blue-400">var</span> config = <span class="text-blue-400">new</span> <span class="text-yellow-300">WinnowConfig</span> { <span class="text-sky-300">Environment</span> = <span class="text-green-400">"Production"</span> };
<span class="text-blue-400">var</span> winnow = <span class="text-blue-400">new</span> <span class="text-yellow-300">WinnowSdkClient</span>(<span class="text-green-400">"wm_live_..."</span>, config);`
    },
    unity: {
        label: 'Unity',
        code: `using UnityEngine;
using Winnow.Sdk.DotNet.Unity;

public class GameInit : MonoBehaviour
{
    void Start()
    {
        var winnow = gameObject.AddComponent<WinnowUnity>();
        winnow.apiKey = "wm_live_...";
    }
}`,
        html: `<span class="text-blue-400">using</span> <span class="text-slate-300">UnityEngine</span>;
<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Sdk.DotNet.Unity</span>;

<span class="text-purple-400">public class</span> <span class="text-yellow-300">GameInit</span> : <span class="text-yellow-300">MonoBehaviour</span>
{
    <span class="text-purple-400">void</span> <span class="text-blue-400">Start</span>()
    {
        <span class="text-blue-400">var</span> winnow = gameObject.<span class="text-blue-400">AddComponent</span>&lt;<span class="text-yellow-300">WinnowUnity</span>&gt;();
        winnow.<span class="text-sky-300">apiKey</span> = <span class="text-green-400">"wm_live_..."</span>;
    }
}`
    },
    godot: {
        label: 'Godot',
        code: `// Ensure WinnowGodot is in Project -> AutoLoad
using Godot;
using Winnow.Sdk.DotNet.GodotNode;

public override void _Ready()
{
    var winnow = new WinnowGodot { ApiKey = "wm_live_..." };
    AddChild(winnow);
}`,
        html: `<span class="text-slate-500">// Ensure WinnowGodot is in Project -&gt; AutoLoad</span>
<span class="text-blue-400">using</span> <span class="text-slate-300">Godot</span>;
<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Sdk.DotNet.GodotNode</span>;

<span class="text-purple-400">public override void</span> <span class="text-blue-400">_Ready</span>()
{
    <span class="text-blue-400">var</span> winnow = <span class="text-blue-400">new</span> <span class="text-yellow-300">WinnowGodot</span> { <span class="text-sky-300">ApiKey</span> = <span class="text-green-400">"wm_live_..."</span> };
    <span class="text-blue-400">AddChild</span>(winnow);
}`
    }
};

export function Integration() {
    const [activeTab, setActiveTab] = useState<'js' | 'react' | 'csharp' | 'unity' | 'godot'>('js');
    const [copied, setCopied] = useState(false);

    const handleCopy = () => {
        navigator.clipboard.writeText(codeSnippets[activeTab].code);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    return (
        <section className="bg-slate-50 dark:bg-slate-950 py-20 border-y border-slate-200 dark:border-slate-800 transition-colors">
            <div className="container mx-auto px-4 md:px-6">
                <div className="text-center mb-12">
                    <h2 className="text-3xl font-bold tracking-tighter md:text-4xl text-slate-900 dark:text-white">
                        Drop it in. <span className="text-brand-gradient">Debug in production.</span>
                    </h2>
                    <p className="mt-4 text-lg text-slate-600 dark:text-slate-400">
                        Get started in minutes with our lightweight SDKs.
                    </p>
                </div>

                {/* Headless Code Container */}
                <div className="max-w-4xl mx-auto rounded-2xl overflow-hidden shadow-2xl bg-[#0F172A] ring-1 ring-white/10">
                    {/* Tab Bar */}
                    <div className="flex items-center justify-between px-2 bg-white/5 border-b border-white/10">
                        <div className="flex space-x-1">
                            {Object.entries(codeSnippets).map(([key, snippet]) => (
                                <button
                                    key={key}
                                    onClick={() => setActiveTab(key as any)}
                                    className={`px-4 py-3 text-sm font-medium transition-all relative ${activeTab === key
                                        ? 'text-white'
                                        : 'text-slate-400 hover:text-white hover:bg-white/5'
                                        }`}
                                >
                                    {snippet.label}
                                    {activeTab === key && (
                                        <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-500 rounded-full" />
                                    )}
                                </button>
                            ))}
                        </div>

                        {/* Copy Button */}
                        <button
                            onClick={handleCopy}
                            className="p-2 mr-2 text-slate-400 hover:text-white transition-colors rounded-md hover:bg-white/10"
                            aria-label="Copy code"
                            title="Copy to clipboard"
                        >
                            {copied ? <Check className="w-4 h-4 text-green-400" /> : <Copy className="w-4 h-4" />}
                        </button>
                    </div>

                    {/* Code Content */}
                    <div className="p-6 md:p-8 font-mono text-sm md:text-base leading-relaxed overflow-x-auto text-slate-300">
                        <pre>
                            <code dangerouslySetInnerHTML={{ __html: codeSnippets[activeTab].html }} />
                        </pre>
                    </div>
                </div>
            </div>
        </section>
    );
}
