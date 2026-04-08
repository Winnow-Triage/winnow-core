import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { useNavigate } from "react-router-dom";

interface UpgradeRequiredModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title?: string;
  description?: string;
}

export function UpgradeRequiredModal({
  open,
  onOpenChange,
  title = "Upgrade Required",
  description = "You have reached your AI limit for this billing cycle. Please upgrade your plan to continue using this feature.",
}: UpgradeRequiredModalProps) {
  const navigate = useNavigate();

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="rounded-2xl border-white/10 backdrop-blur-2xl bg-background/80 shadow-2xl">
        <AlertDialogHeader>
          <AlertDialogTitle className="text-2xl font-black tracking-tight text-amber-500">
            {title}
          </AlertDialogTitle>
          <AlertDialogDescription className="text-foreground/70 leading-relaxed font-medium">
            {description}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="mt-6">
          <AlertDialogCancel className="rounded-xl border-white/10 hover:bg-white/5 font-bold transition-all">
            Cancel
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={() => navigate("/settings?tab=billing")}
            className="rounded-xl bg-amber-600 hover:bg-amber-700 shadow-lg shadow-amber-600/20 font-bold transition-all text-white"
          >
            View Plans
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
