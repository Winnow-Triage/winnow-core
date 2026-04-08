import { useQuery, useMutation } from "@tanstack/react-query";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { toast } from "sonner";

interface ClusterExportMenuProps {
  clusterId: string;
  projectId: string;
  onExport: () => void;
}

export function ClusterExportMenu({
  clusterId,
  projectId,
  onExport,
}: ClusterExportMenuProps) {
  const { data: integrations } = useQuery<
    { id: string; provider: string; name: string }[]
  >({
    queryKey: ["integrations", projectId],
    queryFn: async () => {
      const { data } = await api.get(`/projects/${projectId}/integrations`);
      return data;
    },
    retry: false,
  });

  const exportMutation = useMutation({
    mutationFn: async (configId: string) => {
      const { data } = await api.post(`/clusters/${clusterId}/export`, {
        configId,
      });
      return data;
    },
    onSuccess: (data: { externalUrl?: string }) => {
      toast.success("Cluster exported successfully");
      onExport();
      if (data?.externalUrl) {
        window.open(data.externalUrl, "_blank");
      }
    },
    onError: (error: unknown) => {
      const axiosError = error as { response?: { data?: { error?: string } }; message?: string };
      const displayMsg =
        axiosError.response?.data?.error || axiosError.message || "Unknown error";
      toast.error("Export Failed", { description: displayMsg });
    },
  });

  if (!integrations || integrations.length === 0) {
    return (
      <Button
        variant="outline"
        className="rounded-xl px-6 h-12 shadow-lg transition-all border-white/10 bg-white/5 backdrop-blur-md self-end opacity-50 cursor-not-allowed"
        disabled
        title="No integration configured — add one in Project Settings"
      >
        Export
      </Button>
    );
  }

  if (integrations.length === 1) {
    return (
      <Button
        variant="outline"
        className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
        onClick={() => exportMutation.mutate(integrations[0].id)}
        disabled={exportMutation.isPending}
      >
        {exportMutation.isPending
          ? "Exporting..."
          : `Export to ${integrations[0].provider}`}
      </Button>
    );
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className="rounded-xl px-6 h-12 shadow-lg hover:shadow-xl transition-all border-white/10 bg-white/5 backdrop-blur-md self-end"
          disabled={exportMutation.isPending}
        >
          {exportMutation.isPending ? "Exporting..." : "Export To..."}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {integrations.map((config) => (
          <DropdownMenuItem
            key={config.id}
            onClick={() => exportMutation.mutate(config.id)}
          >
            Export to {config.name}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
