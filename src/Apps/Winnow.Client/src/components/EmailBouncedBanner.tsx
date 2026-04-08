import { useAuth } from "@/hooks/use-auth";
import { AlertCircle } from "lucide-react";
import { Link } from "react-router-dom";

export default function EmailBouncedBanner() {
  const { user } = useAuth();

  if (!user || user.emailBounced !== true) {
    return null;
  }

  return (
    <div className="bg-destructive/15 border-b border-destructive/20 text-destructive-foreground px-4 py-2 flex items-center justify-center gap-4 text-sm font-medium">
      <div className="flex items-center gap-2">
        <AlertCircle className="h-4 w-4" />
        <span>
          Warning: Delivery to your email address has failed. Please update your email address to avoid account suspension.
        </span>
      </div>
      <Link 
        to="/settings/user" 
        className="text-primary hover:underline font-semibold"
      >
        Update Email
      </Link>
    </div>
  );
}
