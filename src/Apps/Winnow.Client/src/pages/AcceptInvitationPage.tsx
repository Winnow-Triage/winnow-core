import React, { useState, useEffect } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { PasswordRules } from "@/components/PasswordRules";
import { validatePassword } from "@/lib/auth-utils";

export default function AcceptInvitationPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get("token");

  const [loading, setLoading] = useState(true);
  const [invitation, setInvitation] = useState<{
    email: string;
    organizationName: string;
  } | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [backendErrors, setBackendErrors] = useState<string[]>([]);

  const isPasswordValid = validatePassword(password);
  const doPasswordsMatch = password === confirmPassword && password.length > 0;

  useEffect(() => {
    const fetchInvitation = async () => {
      if (!token) {
        setLoading(false);
        return;
      }

      try {
        const { data } = await api.get(`/invitations/${token}`);
        setInvitation(data);
      } catch (error) {
        console.error("Failed to fetch invitation:", error);
        toast.error("Invalid or expired invitation link.");
      } finally {
        setLoading(false);
      }
    };

    fetchInvitation();
  }, [token]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBackendErrors([]);

    if (!token || !firstName || !lastName || !password) return;
    if (!isPasswordValid) {
      toast.error("Please ensure your password meets all requirements.");
      return;
    }

    setIsSubmitting(true);
    try {
      await api.post(`/invitations/${token}/accept`, {
        token,
        firstName,
        lastName,
        password,
      });
      toast.success("Account created! You can now log in.");
      navigate("/login");
    } catch (err: unknown) {
      const e = err as { response?: { data?: { errors?: Record<string, string[]> } } };
      const serverErrors = e.response?.data?.errors;
      if (serverErrors) {
        const messages = Object.values(serverErrors).flat();
        setBackendErrors(messages);
        messages.forEach((msg) => toast.error(msg));
      } else {
        toast.error("Failed to complete registration. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="flex h-screen w-screen items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (!invitation) {
    return (
      <div className="flex h-screen w-screen items-center justify-center p-4">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle>Invalid Invitation</CardTitle>
            <CardDescription>
              This invitation link is invalid, has expired, or has already been
              used.
            </CardDescription>
          </CardHeader>
          <CardFooter>
            <Button className="w-full" onClick={() => navigate("/login")}>
              Go to Login
            </Button>
          </CardFooter>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-md shadow-lg border-t-4 border-t-primary">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl font-bold">
            Join {invitation.organizationName}
          </CardTitle>
          <CardDescription>
            Complete your profile to join the organization on Winnow.
          </CardDescription>
        </CardHeader>
        <form onSubmit={handleSubmit}>
          <CardContent className="space-y-6">
            {backendErrors.length > 0 && (
              <div className="p-3 text-sm bg-destructive/10 text-destructive rounded-md border border-destructive/20 font-medium">
                <ul className="list-disc list-inside space-y-1">
                  {backendErrors.map((err, i) => (
                    <li key={i}>{err}</li>
                  ))}
                </ul>
              </div>
            )}

            <div className="space-y-2">
              <Label
                htmlFor="email"
                className="text-xs uppercase tracking-wider text-muted-foreground font-bold"
              >
                Email Address
              </Label>
              <Input
                id="email"
                value={invitation.email}
                disabled
                className="bg-muted font-medium"
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">First Name</Label>
                <Input
                  id="firstName"
                  placeholder="Jane"
                  required
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  className="focus-visible:ring-primary"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Last Name</Label>
                <Input
                  id="lastName"
                  placeholder="Doe"
                  required
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  className="focus-visible:ring-primary"
                />
              </div>
            </div>
            <div className="space-y-3">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="focus-visible:ring-primary"
              />

              <PasswordRules password={password} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <Input
                id="confirmPassword"
                type="password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className={cn(
                  "focus-visible:ring-primary",
                  confirmPassword &&
                    !doPasswordsMatch &&
                    "border-destructive focus-visible:ring-destructive",
                )}
              />
              {confirmPassword && !doPasswordsMatch && (
                <p className="text-[10px] font-bold text-destructive uppercase tracking-tight">
                  Passwords do not match
                </p>
              )}
            </div>
          </CardContent>
          <CardFooter>
            <Button
              className="w-full font-bold h-11"
              type="submit"
              disabled={isSubmitting || !isPasswordValid || !doPasswordsMatch}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Creating Account...
                </>
              ) : (
                "Complete Registration"
              )}
            </Button>
          </CardFooter>
        </form>
      </Card>
    </div>
  );
}
