import { useState } from 'react';
import { api } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PlayCircle, AlertTriangle } from 'lucide-react';

export default function DebugConsole() {
    const [count, setCount] = useState(5);
    const [topic, setTopic] = useState("Login Failure");
    const [isLoading, setIsLoading] = useState(false);
    const [message, setMessage] = useState<string | null>(null);

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
                        <CardTitle>Traffic Simulator</CardTitle>
                        <CardDescription>
                            Generate synthetic tickets to test AI clustering and pipeline throughput.
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
