import React, { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { toast } from "sonner";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Shield, ShieldAlert, Key, Edit, Trash, Plus, Loader2, AlertCircle } from "lucide-react";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Checkbox } from "@/components/ui/checkbox";

export interface Role {
  id: string;
  name: string;
  permissions?: {
    id: string;
    name: string;
    description: string;
  }[];
}

type PermissionDto = {
  id: string;
  name: string;
  description: string | null;
};

type RoleDto = {
  id: string;
  name: string;
  isSystemRole: boolean;
  permissions: PermissionDto[];
};

export function RolesManager({ organizationId }: { organizationId?: string }) {
  const { data: roles, isLoading: isRolesLoading, error: rolesError, refetch: refetchRoles } = useQuery<{ roles: RoleDto[] }>({
    queryKey: ["roles", organizationId],
    queryFn: async () => {
      const { data } = await api.get(`/organizations/${organizationId}/roles`);
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const { data: permissionsData, isLoading: isPermissionsLoading, error: permsError } = useQuery<{ permissions: PermissionDto[] }>({
    queryKey: ["permissions", organizationId],
    queryFn: async () => {
      const { data } = await api.get(`/organizations/${organizationId}/permissions`);
      return data;
    },
    enabled: !!organizationId,
    retry: 0,
  });

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);

  const handleOpenCreateModal = () => {
    setEditingRole(null);
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (role: RoleDto) => {
    setEditingRole(role);
    setIsModalOpen(true);
  };

  const handleDeleteRole = async (roleId: string) => {
    try {
      await api.delete(`/organizations/${organizationId}/roles/${roleId}`);
      toast.success("Role deleted successfully");
      await refetchRoles();
    } catch (error: unknown) {
      const e = error as { response?: { status?: number; data?: { errors?: Array<{ code: string }> } } };
      if (e.response?.status === 400 && e.response?.data?.errors?.find((err) => err.code === "RoleInUse")) {
        toast.error("Cannot delete role because it is assigned to one or more members.");
      } else {
        toast.error("Failed to delete role");
      }
    }
  };

  if (isRolesLoading || isPermissionsLoading) {
    return (
      <div className="py-20 text-center text-muted-foreground">
        <Loader2 className="h-8 w-8 animate-spin mx-auto mb-2 opacity-50" />
        Loading roles...
      </div>
    );
  }

  if (rolesError || permsError) {
    const error = rolesError || permsError;
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center border rounded-lg bg-muted/20 border-dashed">
        <AlertCircle className="h-8 w-8 text-destructive mb-2" />
        <h4 className="font-semibold">Access Denied</h4>
        <p className="text-sm text-muted-foreground">
          {(error as { response?: { data?: { detail?: string } } }).response?.data?.detail || "You don't have permission to manage roles."}
        </p>
      </div>
    );
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0">
        <div>
          <CardTitle>Roles & Permissions</CardTitle>
          <CardDescription>
            Manage custom roles and configure permissions for your organization.
          </CardDescription>
        </div>
        <Button onClick={handleOpenCreateModal}>
          <Plus className="h-4 w-4 mr-2" />
          Create Role
        </Button>
      </CardHeader>
      <CardContent>
        <div className="border rounded-md">
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/50 hover:bg-muted/50">
                <TableHead className="w-[200px]">Role Name</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Permissions</TableHead>
                <TableHead className="text-right">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {roles?.roles?.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground italic">
                    No roles found.
                  </TableCell>
                </TableRow>
              ) : (
                roles?.roles?.map((role) => (
                  <TableRow key={role.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {role.isSystemRole ? (
                          <ShieldAlert className="h-4 w-4 text-emerald-500" />
                        ) : (
                          <Shield className="h-4 w-4 text-blue-500" />
                        )}
                        <span className="font-semibold text-sm">{role.name}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      {role.isSystemRole ? (
                        <Badge variant="outline" className="text-emerald-600 border-emerald-200 bg-emerald-50 dark:bg-emerald-950 dark:border-emerald-800">
                          System Role
                        </Badge>
                      ) : (
                        <Badge variant="outline" className="text-blue-600 border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
                          Custom Role
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap gap-1">
                        {role.permissions.slice(0, 3).map((p) => (
                          <Badge key={p.id} variant="secondary" className="text-[10px] font-mono whitespace-nowrap">
                            {p.name}
                          </Badge>
                        ))}
                        {role.permissions.length > 3 && (
                          <Badge variant="secondary" className="text-[10px]">
                            +{role.permissions.length - 3} more
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      {!role.isSystemRole && (
                        <div className="flex items-center justify-end gap-2">
                          <Button variant="ghost" size="sm" onClick={() => handleOpenEditModal(role)}>
                            <Edit className="h-4 w-4" />
                          </Button>
                          <AlertDialog>
                            <AlertDialogTrigger asChild>
                              <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive hover:bg-destructive/10">
                                <Trash className="h-4 w-4" />
                              </Button>
                            </AlertDialogTrigger>
                            <AlertDialogContent>
                              <AlertDialogHeader>
                                <AlertDialogTitle>Delete Role</AlertDialogTitle>
                                <AlertDialogDescription>
                                  Are you sure you want to delete the role &quot;{role.name}&quot;? This action cannot be undone.
                                </AlertDialogDescription>
                              </AlertDialogHeader>
                              <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <AlertDialogAction onClick={() => handleDeleteRole(role.id)} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                                  Delete
                                </AlertDialogAction>
                              </AlertDialogFooter>
                            </AlertDialogContent>
                          </AlertDialog>
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {permissionsData?.permissions && (
          <RoleModal
            isOpen={isModalOpen}
            onClose={() => setIsModalOpen(false)}
            organizationId={organizationId!}
            initialRole={editingRole}
            allPermissions={permissionsData.permissions}
            onSuccess={() => {
              setIsModalOpen(false);
              refetchRoles();
            }}
          />
        )}
      </CardContent>
    </Card>
  );
}

function RoleModal({
  isOpen,
  onClose,
  organizationId,
  initialRole,
  allPermissions,
  onSuccess,
}: {
  isOpen: boolean;
  onClose: () => void;
  organizationId: string;
  initialRole: RoleDto | null;
  allPermissions: PermissionDto[];
  onSuccess: () => void;
}) {
  const isEditing = !!initialRole;
  const [roleName, setRoleName] = useState("");
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  React.useEffect(() => {
    if (isOpen) {
      if (isEditing && initialRole) {
        setRoleName(initialRole.name);
        setSelectedPermissions(initialRole.permissions.map(p => p.id));
      } else {
        setRoleName("");
        setSelectedPermissions([]);
      }
    }
  }, [isOpen, isEditing, initialRole]);

  const groupedPermissions = React.useMemo(() => {
    const groups: Record<string, PermissionDto[]> = {};
    allPermissions.forEach(p => {
      const category = p.name.includes(':') ? p.name.split(':')[0] : 'Other';
      const capitalized = category.charAt(0).toUpperCase() + category.slice(1);
      if (!groups[capitalized]) groups[capitalized] = [];
      groups[capitalized].push(p);
    });
    return groups;
  }, [allPermissions]);

  const handleSubmit = async () => {
    if (!roleName.trim()) return;
    setIsSubmitting(true);
    try {
      if (isEditing) {
        await api.put(`/organizations/${organizationId}/roles/${initialRole.id}`, {
          name: roleName.trim(),
          permissionIds: selectedPermissions,
        });
        toast.success("Role updated successfully");
      } else {
        await api.post(`/organizations/${organizationId}/roles`, {
          name: roleName.trim(),
          permissionIds: selectedPermissions,
        });
        toast.success("Role created successfully");
      }
      onSuccess();
    } catch {
      toast.error(`Failed to ${isEditing ? "update" : "create"} role`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleTogglePermission = (permissionId: string) => {
    setSelectedPermissions(prev =>
      prev.includes(permissionId)
        ? prev.filter(id => id !== permissionId)
        : [...prev, permissionId]
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[600px] p-0 flex flex-col overflow-hidden max-h-[85vh]">
        <DialogHeader className="p-6 pb-2">
          <DialogTitle>{isEditing ? "Edit Role" : "Create Role"}</DialogTitle>
          <DialogDescription>
            {isEditing ? "Update role name and permissions." : "Define a new custom role and assign permissions."}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 p-6 overflow-y-auto">
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Role Name</Label>
            <Input
              placeholder="e.g., Billing Manager"
              value={roleName}
              onChange={(e) => setRoleName(e.target.value)}
              className="bg-muted/30"
            />
          </div>

          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <Label className="text-sm font-semibold flex items-center gap-2">
                <Key className="h-4 w-4" />
                Permissions
              </Label>
              <div className="text-xs text-muted-foreground">
                {selectedPermissions.length} selected
              </div>
            </div>

            <div className="rounded-md border p-4 bg-muted/10 h-[300px] overflow-y-auto space-y-6">
              {Object.entries(groupedPermissions).map(([category, perms]) => (
                <div key={category} className="space-y-3">
                  <h4 className="font-medium text-sm text-foreground/80 border-b pb-1">
                    {category}
                  </h4>
                  <div className="grid gap-3 sm:grid-cols-2">
                    {perms.map((permission) => (
                      <div key={permission.id} className="flex items-start space-x-3 p-2 rounded-lg hover:bg-muted/50 transition-colors">
                        <Checkbox
                          id={permission.id}
                          checked={selectedPermissions.includes(permission.id)}
                          onCheckedChange={() => handleTogglePermission(permission.id)}
                          className="mt-1"
                        />
                        <div className="space-y-1">
                          <Label
                            htmlFor={permission.id}
                            className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
                          >
                            {permission.name}
                          </Label>
                          <p className="text-xs text-muted-foreground leading-snug">
                            {permission.description || "Grants access to specific system functionality."}
                          </p>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <DialogFooter className="p-6 bg-muted/30 border-t">
          <Button variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={isSubmitting || !roleName.trim()}>
            {isSubmitting ? "Saving..." : "Save Role"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
