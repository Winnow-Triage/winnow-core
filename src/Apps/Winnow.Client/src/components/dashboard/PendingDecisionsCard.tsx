import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  CardFooter,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { GitPullRequest } from "lucide-react";
import { Link } from "react-router-dom";

interface PendingDecisionsCardProps {
  count: number;
}

export function PendingDecisionsCard({ count }: PendingDecisionsCardProps) {
  return (
    <Card className="h-full flex flex-col justify-between bg-blue-500/5">
      <div>
        <CardHeader className="pb-2 text-blue-600 dark:text-blue-400">
          <CardTitle className="text-sm font-medium flex items-center gap-2">
            <GitPullRequest className="w-4 h-4" />
            Pending Decisions
          </CardTitle>
        </CardHeader>
        <CardContent className="pt-4">
          <div className="text-3xl font-bold">{count}</div>
          <p className="text-xs text-blue-600/80 dark:text-muted-foreground mt-1">
            Suggestions waiting for human confirmation.
          </p>
        </CardContent>
      </div>
      <CardFooter>
        <Link to="/triage/review" className="w-full">
          <Button
            className="w-full h-8 text-xs bg-white border-blue-200 text-blue-600 hover:bg-blue-50 dark:bg-transparent dark:border-blue-500 dark:text-blue-500 dark:hover:bg-blue-500/10 transition-colors"
            variant="outline"
            disabled={count === 0}
          >
            Review {count} Suggestions
          </Button>
        </Link>
      </CardFooter>
    </Card>
  );
}
