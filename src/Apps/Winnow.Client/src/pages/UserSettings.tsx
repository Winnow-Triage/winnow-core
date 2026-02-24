import React, { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { getAccountDetails, updateAccountDetails, changePassword } from "@/lib/api";
import { Card, CardContent, CardDescription, CardHeader, CardTitle, CardFooter } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "sonner";
import { useSearchParams } from "react-router-dom";

export default function UserSettings() {
    const [searchParams, setSearchParams] = useSearchParams();
    const currentTab = searchParams.get("tab") || "profile";

    const { data: user, isLoading, refetch } = useQuery({
        queryKey: ["account-details"],
        queryFn: getAccountDetails,
    });

    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [isUpdatingProfile, setIsUpdatingProfile] = useState(false);

    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [isChangingPassword, setIsChangingPassword] = useState(false);

    useEffect(() => {
        if (user) {
            setFullName(user.fullName);
            setEmail(user.email);
        }
    }, [user]);

    const handleUpdateProfile = async () => {
        const hasChanges = fullName !== user?.fullName || email !== user?.email;
        if (!fullName.trim() || !email.trim() || !hasChanges) return;

        setIsUpdatingProfile(true);
        try {
            await updateAccountDetails(fullName.trim(), email.trim());
            // Update local storage to reflect changes in UI immediately
            const storedUser = JSON.parse(localStorage.getItem("user") || "{}");
            localStorage.setItem("user", JSON.stringify({
                ...storedUser,
                name: fullName.trim(),
                email: email.trim()
            }));

            await refetch();
            toast.success("Profile updated successfully");
        } catch (error) {
            console.error("Failed to update profile:", error);
            toast.error("Failed to update profile");
        } finally {
            setIsUpdatingProfile(false);
        }
    };

    const handleChangePassword = async (e: React.FormEvent) => {
        e.preventDefault();
        if (newPassword !== confirmPassword) {
            toast.error("New passwords do not match");
            return;
        }

        setIsChangingPassword(true);
        try {
            await changePassword(currentPassword, newPassword);
            toast.success("Password changed successfully");
            setCurrentPassword("");
            setNewPassword("");
            setConfirmPassword("");
        } catch (error) {
            console.error("Failed to change password:", error);
            toast.error("Failed to change password. Please check your current password.");
        } finally {
            setIsChangingPassword(false);
        }
    };

    return (
        <div className="max-w-2xl w-full mx-auto py-8">
            <div className="mb-8 font-inter">
                <h1 className="text-3xl font-bold tracking-tight text-foreground">User Settings</h1>
                <p className="text-muted-foreground">Manage your personal profile and security.</p>
            </div>

            <Tabs value={currentTab} onValueChange={(val) => setSearchParams({ tab: val })} className="w-full">
                <TabsList className="grid w-full grid-cols-2 max-w-[400px]">
                    <TabsTrigger value="profile">Profile</TabsTrigger>
                    <TabsTrigger value="security">Security</TabsTrigger>
                </TabsList>

                <TabsContent value="profile" className="mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Profile Details</CardTitle>
                            <CardDescription>Update your public-facing information.</CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="email">Email Address</Label>
                                <Input
                                    id="email"
                                    placeholder="Enter your email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    disabled={isLoading}
                                />
                                <p className="text-xs text-muted-foreground italic">Note: Changing your email will also change your login username.</p>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="fullName">Full Name</Label>
                                <Input
                                    id="fullName"
                                    placeholder="Enter your full name"
                                    value={fullName}
                                    onChange={(e) => setFullName(e.target.value)}
                                    disabled={isLoading}
                                />
                            </div>
                        </CardContent>
                        <CardFooter className="flex justify-end p-4 border-t bg-muted/30">
                            <Button
                                onClick={handleUpdateProfile}
                                disabled={isUpdatingProfile || !fullName.trim() || !email.trim() || (fullName === user?.fullName && email === user?.email)}
                            >
                                {isUpdatingProfile ? "Saving..." : "Save Changes"}
                            </Button>
                        </CardFooter>
                    </Card>
                </TabsContent>

                <TabsContent value="security" className="mt-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Security</CardTitle>
                            <CardDescription>Update your password to keep your account secure.</CardDescription>
                        </CardHeader>
                        <form onSubmit={handleChangePassword}>
                            <CardContent className="space-y-4">
                                <div className="space-y-2">
                                    <Label htmlFor="currentPassword">Current Password</Label>
                                    <Input
                                        id="currentPassword"
                                        type="password"
                                        value={currentPassword}
                                        onChange={(e) => setCurrentPassword(e.target.value)}
                                        required
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="newPassword">New Password</Label>
                                    <Input
                                        id="newPassword"
                                        type="password"
                                        value={newPassword}
                                        onChange={(e) => setNewPassword(e.target.value)}
                                        required
                                    />
                                </div>
                                <div className="space-y-2">
                                    <Label htmlFor="confirmPassword">Confirm New Password</Label>
                                    <Input
                                        id="confirmPassword"
                                        type="password"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        required
                                    />
                                </div>
                            </CardContent>
                            <CardFooter className="flex justify-end p-4 border-t bg-muted/30">
                                <Button
                                    type="submit"
                                    disabled={isChangingPassword || !currentPassword || !newPassword || newPassword !== confirmPassword}
                                >
                                    {isChangingPassword ? "Updating..." : "Update Password"}
                                </Button>
                            </CardFooter>
                        </form>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}
