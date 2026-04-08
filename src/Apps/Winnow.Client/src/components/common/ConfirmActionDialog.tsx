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

interface ConfirmActionDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  onConfirm: () => void;
  actionText?: string;
  cancelText?: string;
  variant?: "default" | "destructive";
}

export function ConfirmActionDialog({
  open,
  onOpenChange,
  title,
  description,
  onConfirm,
  actionText = "Proceed",
  cancelText = "Cancel",
  variant = "default",
}: ConfirmActionDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="rounded-2xl border-white/10 backdrop-blur-2xl bg-background/80 shadow-2xl">
        <AlertDialogHeader>
          <AlertDialogTitle className="text-2xl font-black tracking-tight">
            {title}
          </AlertDialogTitle>
          <AlertDialogDescription className="text-foreground/70 leading-relaxed font-medium">
            {description}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter className="mt-6">
          <AlertDialogCancel className="rounded-xl border-white/10 hover:bg-white/5 font-bold transition-all">
            {cancelText}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={onConfirm}
            className={`rounded-xl shadow-lg font-bold transition-all ${
              variant === "destructive"
                ? "bg-red-600 hover:bg-red-700 shadow-red-600/20"
                : "bg-blue-600 hover:bg-blue-700 shadow-blue-600/20"
            }`}
          >
            {actionText}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
