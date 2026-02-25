import { HelpCircle, Info, Github, ExternalLink } from "lucide-react"
import { useQuery } from "@tanstack/react-query"
import { api } from "@/lib/api"
import { Button } from "@/components/ui/button"
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from "@/components/ui/dialog"

export function AboutDialog() {
    const { data: organization } = useQuery({
        queryKey: ['current-organization'],
        queryFn: async () => {
            const { data } = await api.get('/organizations/current');
            return data;
        }
    });

    const planName = organization?.subscriptionTier || "Free";

    return (
        <Dialog>
            <DialogTrigger asChild>
                <Button variant="outline" size="icon" className="h-9 w-9">
                    <HelpCircle className="h-[1.2rem] w-[1.2rem]" />
                    <span className="sr-only">About Winnow</span>
                </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-width-[425px]">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2 text-2xl font-bold tracking-tight">
                        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600">
                            <span className="text-white text-xs font-black italic">W</span>
                        </div>
                        <span className="bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
                            Winnow Triage
                        </span>
                    </DialogTitle>
                    <DialogDescription className="pt-2 text-base">
                        Intelligent error grouping and triage for modern SaaS.
                    </DialogDescription>
                </DialogHeader>
                <div className="grid gap-4 py-4">
                    <div className="space-y-4">
                        <p className="text-sm text-muted-foreground leading-relaxed">
                            Winnow helps engineering teams cut through the noise by automatically clustering
                            related errors, prioritizing critical issues, and providing AI-powered recovery
                            suggestions.
                        </p>

                        <div className="rounded-lg border bg-muted/50 p-3 space-y-2">
                            <div className="flex items-center justify-between text-xs">
                                <span className="font-semibold text-foreground uppercase tracking-wider">Version</span>
                                <span className="text-muted-foreground">0.1.0-alpha (MVP)</span>
                            </div>
                            <div className="flex items-center justify-between text-xs">
                                <span className="font-semibold text-foreground uppercase tracking-wider">Plan</span>
                                <span className="text-blue-600 font-medium">{planName}</span>
                            </div>
                        </div>

                        <div className="flex flex-col gap-2">
                            <Button variant="outline" size="sm" className="justify-start gap-2" asChild>
                                <a href="https://github.com/winnow-triage" target="_blank" rel="noreferrer">
                                    <Github className="h-4 w-4" />
                                    Documentation
                                    <ExternalLink className="ml-auto h-3 w-3 opacity-50" />
                                </a>
                            </Button>
                            <Button variant="ghost" size="sm" className="justify-start gap-2" asChild>
                                <a href="https://winnowtriage.com" target="_blank" rel="noreferrer">
                                    <Info className="h-4 w-4" />
                                    Visit Website
                                    <ExternalLink className="ml-auto h-3 w-3 opacity-50" />
                                </a>
                            </Button>
                        </div>
                    </div>
                </div>
                <div className="flex justify-center border-t pt-4">
                    <p className="text-[10px] text-muted-foreground uppercase tracking-widest">
                        Handcrafted by Winnow Triage, LLC • &copy; {new Date().getFullYear()}
                    </p>
                </div>
            </DialogContent>
        </Dialog>
    )
}
