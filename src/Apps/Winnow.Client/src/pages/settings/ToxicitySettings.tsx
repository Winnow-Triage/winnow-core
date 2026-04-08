import { useState } from "react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  ShieldAlert,
  Skull,
  MessageSquareQuote,
  Flame,
  UserCheck,
  Eye,
  Activity,
} from "lucide-react";
import type { Organization } from "@/types";

interface ToxicitySettingsProps {
  organization: Organization | undefined;
}

export function ToxicitySettings({ organization }: ToxicitySettingsProps) {
  // Toxicity Settings State
  const [toxicityEnabled, setToxicityEnabled] = useState(organization?.toxicityFilterEnabled ?? true);
  const [toxicityThresholds] = useState(organization?.toxicityLimits ?? {
    profanity: 0.8,
    hateSpeech: 0.1,
    violence: 0.1,
    insult: 0.1,
    harassment: 0.1,
    sexual: 0.1,
    graphic: 0.1,
    overall: 0.1,
  });


  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <CardTitle>Toxicity Filtering</CardTitle>
              <Badge variant="outline" className="text-[10px] h-5 px-2 bg-blue-500/10 text-blue-500 border-blue-500/20 uppercase tracking-tighter">Coming Soon</Badge>
            </div>
            <CardDescription>
              Configure automated content moderation for all incoming reports.
              <span className="block mt-1 text-[10px] text-muted-foreground italic">
                Note: A higher percentage allows more content through, while a lower percentage results in more aggressive blocking.
              </span>
            </CardDescription>
          </div>
          <div className="flex items-center gap-2 bg-muted/50 p-2 rounded-xl border border-border/50 opacity-50">
            <Checkbox
              id="tox-enabled"
              disabled
              checked={toxicityEnabled}
              onCheckedChange={(checked) => setToxicityEnabled(checked as boolean)}
            />
            <Label htmlFor="tox-enabled" className="text-xs font-semibold uppercase tracking-wider cursor-not-allowed">
              {toxicityEnabled ? "Active" : "Disabled"}
            </Label>
          </div>
        </div>
      </CardHeader>
      <CardContent className="relative p-6">
        <div className="space-y-6 transition-opacity duration-300 opacity-40 pointer-events-none blur-[2px]">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {[
              { id: "profanity", label: "Profanity", icon: MessageSquareQuote, color: "text-blue-500" },
              { id: "hateSpeech", label: "Hate Speech", icon: Skull, color: "text-red-500" },
              { id: "violence", label: "Violence", icon: Flame, color: "text-orange-500" },
              { id: "insult", label: "Insult", icon: UserCheck, color: "text-purple-500" },
              { id: "harassment", label: "Harassment", icon: ShieldAlert, color: "text-amber-500" },
              { id: "sexual", label: "Sexual Content", icon: Eye, color: "text-pink-500" },
              { id: "graphic", label: "Graphic Content", icon: Skull, color: "text-zinc-500" },
              { id: "overall", label: "Overall Toxicity", icon: Activity, color: "text-emerald-500" },
            ].map((item) => (
              <div key={item.id} className="space-y-3 p-4 rounded-2xl bg-muted/30 border border-border/40">
                <div className="flex justify-between items-center mb-1.5">
                  <div className="flex items-center gap-1.5">
                    <item.icon className="h-3.5 w-3.5 text-blue-500/80" />
                    <Label className="font-semibold">{item.label}</Label>
                  </div>
                  <span className="text-xs font-mono bg-background px-2 py-0.5 rounded border border-border/50">
                    {Math.round((toxicityThresholds as unknown as Record<string, number>)[item.id] * 100)}%
                  </span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="1"
                  step="0.01"
                  disabled
                  value={(toxicityThresholds as unknown as Record<string, number>)[item.id]}
                  className="w-full h-1.5 bg-muted rounded-lg appearance-none cursor-not-allowed accent-blue-500/50"
                  title="Disabled until V1.1"
                />
                <div className="flex justify-between text-[8px] uppercase tracking-tighter font-bold text-muted-foreground/60">
                  <span>Strict (Block)</span>
                  <span>Sensitive</span>
                  <span>Relaxed (Allow)</span>
                </div>
              </div>
            ))}
          </div>
        </div>
        
        {/* Coming Soon Overlay - Outside the blur container */}
        <div className="absolute inset-0 z-20 flex items-center justify-center p-6 pointer-events-none">
          <div className="bg-background/95 backdrop-blur-sm border border-indigo-500/20 px-5 py-2.5 rounded-2xl shadow-2xl flex items-center gap-3 animate-in zoom-in-95 duration-300">
            <div className="bg-indigo-500/10 p-1.5 rounded-lg border border-indigo-500/20">
              <ShieldAlert className="w-4 h-4 text-indigo-500" />
            </div>
            <div className="flex flex-col">
              <span className="text-[10px] font-bold text-indigo-500 uppercase tracking-widest leading-tight">Coming Soon</span>
              <span className="text-xs font-semibold text-foreground/80 lowercase italic">Available in V1.1</span>
            </div>
          </div>
        </div>
      </CardContent>
      <CardFooter className="flex items-center justify-end p-4 px-6 border-t bg-muted/50 rounded-b-3xl mt-4">
        <Button disabled>Save Toxicity Settings</Button>
      </CardFooter>
    </Card>
  );
}
