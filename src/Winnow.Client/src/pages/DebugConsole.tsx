import { useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PlayCircle, AlertTriangle, Sparkles, RefreshCw } from 'lucide-react';

export default function DebugConsole() {
    const [count, setCount] = useState(5);
    const [topic, setTopic] = useState("Login Failure");
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState<string | null>(null);

    // LLM Mock Gen State
    const [mockCount, setMockCount] = useState(10);
    const [scenario, setScenario] = useState("A series of login issues where users receive 'Invalid Credentials' but know their password is correct.");
    const [isMockLoading, setIsMockLoading] = useState(false);
    const [mockMessage, setMockMessage] = useState<string | null>(null);

    const handleSimulate = async () => {
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

    const handleGenerateMock = async () => {
        setIsMockLoading(true);
        setMockMessage(null);
        try {
            const { data } = await api.post('/tickets/generate-mock', { count: mockCount, scenario });
            setMockMessage(data.message);
        } catch (error) {
            console.error(error);
            setMockMessage("Failed to generate mock tickets.");
        } finally {
            setIsMockLoading(false);
        }
    };

    return (
        <div className="flex flex-col gap-6 max-w-4xl mx-auto w-full p-4">
            <h1 className="text-2xl font-bold tracking-tight text-red-500 flex items-center gap-2">
                <AlertTriangle className="h-6 w-6" /> Debug Console
            </h1>
            <p className="text-muted-foreground">
                Tools for developers to test system behavior. Not for production use.
            </p>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Sparkles className="h-5 w-5 text-amber-500" /> AI Mock Generator
                        </CardTitle>
                        <CardDescription>
                            Use the LLM to generate realistic, high-quality support tickets based on a specific scenario.
                        </CardDescription>
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
                            <Label htmlFor="mockCount">Number of Tickets</Label>
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
                            {isMockLoading ? "Generating..." : "Generate LLM Tickets"}
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
                            <Label htmlFor="count">Number of Tickets</Label>
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
