import { useState } from 'react';
import { api } from '@/lib/api';
import { useProject } from '@/context/ProjectContext';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PlayCircle, AlertTriangle, Sparkles, RefreshCw, Folder } from 'lucide-react';
import { PageTitle } from '@/components/ui/page-title';

export default function DebugConsole() {
    const { currentProject } = useProject();
    const [count, setCount] = useState(5);
    const [topic, setTopic] = useState("Login Failure");
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState<string | null>(null);

    // LLM Mock Gen State
    const [mockCount, setMockCount] = useState(10);
    const [scenario, setScenario] = useState("A series of login issues where users receive 'Invalid Credentials' but know their password is correct.");
    const [isMockLoading, setIsMockLoading] = useState(false);
    const [mockMessage, setMockMessage] = useState<string | null>(null);

    const handleGenerateMock = async () => {
        if (!currentProject) {
            setMockMessage("Please select a project first.");
            return;
        }
        setIsMockLoading(true);
        setMockMessage(null);
        try {
            const { data } = await api.post('/reports/generate-mock', { count: mockCount, scenario });
            setMockMessage(data.message);
        } catch (error) {
            console.error(error);
            setMockMessage("Failed to generate mock reports.");
        } finally {
            setIsMockLoading(false);
        }
    };

    const handleSimulate = async () => {
        if (!currentProject) {
            setMessage("Please select a project first.");
            return;
        }
        setIsLoading(true);
        setMessage(null);
        try {
            const { data } = await api.post('/debug/simulate-traffic', { count, topic });
            setMessage(data.message);
        } catch (error) {
            console.error(error);
            setMessage("Failed to simulate traffic.");
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="flex flex-col gap-6 max-w-4xl mx-auto w-full">
            <div className="flex flex-col gap-1 text-red-500">
                <PageTitle>Debug Console</PageTitle>
                <p className="text-muted-foreground flex items-center gap-2">
                    <AlertTriangle className="h-4 w-4" /> Tools for developers to test system behavior. Not for production use.
                </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Sparkles className="h-5 w-5 text-amber-500" /> AI Mock Generator
                        </CardTitle>
                        <CardDescription>
                            Use the LLM to generate realistic, high-quality support reports based on a specific scenario.
                        </CardDescription>
                        {currentProject && (
                            <div className="flex items-center gap-2 mt-2 text-sm text-muted-foreground">
                                <Folder className="h-4 w-4" />
                                <span>Reports will be generated for project: <span className="font-semibold">{currentProject.name}</span></span>
                            </div>
                        )}
                    </CardHeader>
                    <CardContent className="flex flex-col gap-4">
                        <div className="grid w-full items-center gap-1.5">
                            <Label htmlFor="scenario">Scenario Context</Label>
                            <Input
                                id="scenario"
                                placeholder="Describe the situation..."
                                value={scenario}
                                onChange={(e) => setScenario(e.target.value)}
                            />
                        </div>

                        <div className="grid w-full max-w-sm items-center gap-1.5">
                            <Label htmlFor="mockCount">Number of Reports</Label>
                            <Input
                                type="number"
                                id="mockCount"
                                value={mockCount}
                                onChange={(e) => setMockCount(parseInt(e.target.value))}
                                min={1}
                                max={20}
                            />
                        </div>

                        <Button onClick={handleGenerateMock} disabled={isMockLoading} className="mt-2" variant="outline">
                            {isMockLoading ? <RefreshCw className="mr-2 h-4 w-4 animate-spin" /> : <Sparkles className="mr-2 h-4 w-4" />}
                            {isMockLoading ? "Generating..." : "Generate LLM Reports"}
                        </Button>

                        {mockMessage && (
                            <div className="p-2 bg-muted rounded text-sm font-mono mt-2 animate-in fade-in border-l-2 border-amber-500">
                                {mockMessage}
                            </div>
                        )}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Basic Traffic Simulator</CardTitle>
                        <CardDescription>
                            Generate synthetic templates to stress test backend pipeline throughput.
                        </CardDescription>
                        {currentProject && (
                            <div className="flex items-center gap-2 mt-2 text-sm text-muted-foreground">
                                <Folder className="h-4 w-4" />
                                <span>Reports will be generated for project: <span className="font-semibold">{currentProject.name}</span></span>
                            </div>
                        )}
                    </CardHeader>
                    <CardContent className="flex flex-col gap-4">
                        <div className="grid w-full max-w-sm items-center gap-1.5">
                            <Label htmlFor="topic">Scenario / Topic</Label>
                            <Select value={topic} onValueChange={setTopic}>
                                <SelectTrigger>
                                    <SelectValue placeholder="Select a topic" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="Login Failure">Login Failure</SelectItem>
                                    <SelectItem value="Database Timeout">Database Timeout</SelectItem>
                                    <SelectItem value="Payment Issue">Payment Issue</SelectItem>
                                    <SelectItem value="Random">Random Mix</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="grid w-full max-w-sm items-center gap-1.5">
                            <Label htmlFor="count">Number of Reports</Label>
                            <Input
                                type="number"
                                id="count"
                                value={count}
                                onChange={(e) => setCount(parseInt(e.target.value))}
                                min={1}
                                max={50}
                            />
                        </div>

                        <Button onClick={handleSimulate} disabled={isLoading} className="mt-2" variant="destructive">
                            {isLoading ? "Simulating..." : (
                                <>
                                    <PlayCircle className="mr-2 h-4 w-4" /> Run Simulation
                                </>
                            )}
                        </Button>

                        {message && (
                            <div className="p-2 bg-muted rounded text-sm font-mono mt-2 animate-in fade-in">
                                {message}
                            </div>
                        )}
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>System Status</CardTitle>
                        <CardDescription>Real-time metrics (Placeholder)</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="text-sm text-muted-foreground">
                            Active Consumers: 1<br />
                            Queue Depth: 0<br />
                            AI Model: Loaded (all-MiniLM-L6-v2)<br />
                            Vector DB: Connected
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
