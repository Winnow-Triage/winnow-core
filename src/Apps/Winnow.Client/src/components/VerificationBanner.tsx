import { useState } from "react";
import { toast } from "sonner";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { AlertTriangle, Loader2 } from "lucide-react";

export default function VerificationBanner() {
  const [isLoading, setIsLoading] = useState(false);
  const [isSent, setIsSent] = useState(false);

  // Retrieve user from localStorage
  const userString = localStorage.getItem("user");
  const user = userString ? JSON.parse(userString) : null;

  if (!user || user.isEmailVerified) {
    return null;
  }

  const handleResend = async () => {
    setIsLoading(true);
    try {
      const response = await api.post("/auth/resend-verification");

      if (response.data.message?.includes("already verified")) {
        toast.success("Email is already verified!");
        setTimeout(() => window.location.reload(), 1000);
        return;
      }

      toast.success("Verification email sent!");
      setIsSent(true);

      // Re-enable after 60 seconds to match rate limit
      setTimeout(() => setIsSent(false), 60000);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { error?: string, message?: string, errors?: { description: string }[] } } };
      const message =
        e.response?.data?.errors?.[0]?.description ||
        e.response?.data?.message ||
        e.response?.data?.error ||
        "Failed to resend verification email.";
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="bg-amber-50 border-b border-amber-200 text-amber-800 px-4 py-2 flex items-center justify-center gap-4 text-sm font-medium">
      <div className="flex items-center gap-2">
        <AlertTriangle className="h-4 w-4" />
        <span>
          Please verify your email address to unlock team collaboration and
          issue exports.
        </span>
      </div>
      <Button
        variant="outline"
        size="sm"
        className="h-7 border-amber-300 bg-amber-100 hover:bg-amber-200 text-amber-900 transition-colors"
        onClick={handleResend}
        disabled={isLoading || isSent}
      >
        {isLoading ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Sending...
          </>
        ) : isSent ? (
          "Sent!"
        ) : (
          "Resend Email"
        )}
      </Button>
    </div>
  );
}
