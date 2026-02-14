import { useState } from 'react';

// Pre-highlighted HTML to strictly control colors and avoid regex bugs
const codeSnippets = {
    js: `<span class="text-purple-400">import</span> { <span class="text-yellow-300">Winnow</span> } <span class="text-purple-400">from</span> <span class="text-green-400">'@winnow/sdk'</span>;

<span class="text-yellow-300">Winnow</span>.<span class="text-blue-400">init</span>({
  <span class="text-sky-300">apiKey</span>: <span class="text-green-400">"wm_live_..."</span>,
  <span class="text-sky-300">release</span>: <span class="text-green-400">"v1.0.0"</span>
});`,

    react: `<span class="text-purple-400">import</span> { <span class="text-yellow-300">WinnowProvider</span> } <span class="text-purple-400">from</span> <span class="text-green-400">'@winnow/react'</span>;

<span class="text-purple-400">export default function</span> <span class="text-blue-400">App</span>() {
  <span class="text-purple-400">return</span> (
    &lt;<span class="text-yellow-300">WinnowProvider</span> <span class="text-sky-300">apiKey</span>=<span class="text-green-400">"wm_live_..."</span>&gt;
      &lt;<span class="text-yellow-300">YourApp</span> /&gt;
    &lt;/<span class="text-yellow-300">WinnowProvider</span>&gt;
  );
}`,

    csharp: `<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Sdk</span>;

<span class="text-blue-400">var</span> builder = <span class="text-yellow-300">WebApplication</span>.CreateBuilder(args);

<span class="text-slate-500">// Add Winnow to the container</span>
builder.Services.<span class="text-yellow-300">AddWinnow</span>(config => {
    config.<span class="text-sky-300">ApiKey</span> = <span class="text-green-400">"wm_live_..."</span>;
});`,

    godot: `<span class="text-blue-400">using</span> <span class="text-slate-300">Winnow.Godot</span>;

<span class="text-purple-400">public override void</span> <span class="text-blue-400">_Ready</span>()
{
    <span class="text-slate-500">// Initialize specifically for game engines</span>
    <span class="text-yellow-300">Winnow</span>.<span class="text-yellow-300">Godot</span>.<span class="text-blue-400">Start</span>(<span class="text-yellow-300">GetTree</span>(), <span class="text-green-400">"wm_live_..."</span>);
}`
};

export function Integration() {
    const [activeTab, setActiveTab] = useState<'js' | 'react' | 'csharp' | 'godot'>('js');

    return (
        <section className="bg-slate-950 py-20 border-y border-slate-900">
            <div className="container mx-auto px-4 md:px-6">
                <div className="text-center mb-12">
                    <h2 className="text-3xl font-bold tracking-tighter md:text-4xl text-white">
                        Drop it in. <span className="text-blue-500">Debug in production.</span>
                    </h2>
                </div>

                <div className="max-w-3xl mx-auto rounded-xl overflow-hidden shadow-2xl border border-slate-800 bg-[#0d1117]">
                    {/* Tabs */}
                    <div className="flex overflow-x-auto border-b border-slate-800 bg-slate-900/50">
                        {Object.keys(codeSnippets).map((key) => (
                            <button
                                key={key}
                                onClick={() => setActiveTab(key as any)}
                                className={`px-6 py-3 text-sm font-medium transition-colors border-b-2 capitalize ${activeTab === key
                                    ? (() => {
                                        switch (key) {
                                            case 'js': return 'border-blue-500 text-blue-400 bg-slate-800/50';
                                            case 'react': return 'border-cyan-500 text-cyan-400 bg-slate-800/50';
                                            case 'csharp': return 'border-purple-500 text-purple-400 bg-slate-800/50';
                                            case 'godot': return 'border-emerald-500 text-emerald-400 bg-slate-800/50';
                                            default: return '';
                                        }
                                    })()
                                    : 'border-transparent text-slate-400 hover:text-slate-200'
                                    }`}
                            >
                                {key === 'csharp' ? 'C# / .NET' : key === 'js' ? 'JavaScript' : key}
                            </button>
                        ))}
                    </div>

                    {/* Code Content */}
                    <div className="p-6 md:p-8 font-mono text-sm md:text-base overflow-x-auto bg-[#0d1117] text-slate-300">
                        <pre>
                            <code dangerouslySetInnerHTML={{ __html: codeSnippets[activeTab] }} />
                        </pre>
                    </div>
                </div>
            </div>
        </section>
    );
}
