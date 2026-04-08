import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useParams, useNavigate } from "react-router-dom";
import { api } from "@/lib/api";
import { toast } from "sonner";
import { ClusterHero } from "@/components/clusters/ClusterHero";
import { AiInsightCard } from "@/components/clusters/AiInsightCard";
import { ImpactedReportsList } from "@/components/clusters/ImpactedReportsList";
import { PageState, LoadingState, ErrorState } from "@/components/layout/PageState";
import { ConfirmActionDialog } from "@/components/common/ConfirmActionDialog";
import { UpgradeRequiredModal } from "@/components/common/UpgradeRequiredModal";

interface ClusterMember {
  id: string;
  title: string;
  message: string;
  status: string;
  createdAt: string;
  confidenceScore?: number;
}

interface ClusterDetailData {
  id: string;
  projectId: string;
  title?: string;
  summary?: string;
  criticalityScore?: number;
  criticalityReasoning?: string;
  isSummarizing: boolean;
  status: string;
  createdAt: string;
  reportCount: number;
  firstSeen?: string;
  lastSeen?: string;
  assignedTo?: string;
  velocity1h: number;
  velocity24h: number;
  reports: ClusterMember[];
}

export default function ClusterDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [isGeneratingSummaryLocal, setIsGeneratingSummaryLocal] = useState(false);
  const [isClearingSummary, setIsClearingSummary] = useState(false);
  const [isUpgradeModalOpen, setIsUpgradeModalOpen] = useState(false);
  const [isResolveConfirmOpen, setIsResolveConfirmOpen] = useState(false);

  const { data: billingStatus } = useQuery({
    queryKey: ["billing-status"],
    queryFn: () => api.get("/billing/status").then((r) => r.data),
  });

  const {
    data: cluster,
    isLoading,
    error,
  } = useQuery<ClusterDetailData>({
    queryKey: ["cluster", id],
    queryFn: async () => {
      const { data } = await api.get(`/clusters/${id}`);
      return data;
    },
    enabled: !!id,
    refetchInterval: (query) => (query.state.data?.isSummarizing ? 3000 : false),
  });

  const isGeneratingSummary = isGeneratingSummaryLocal || cluster?.isSummarizing;

  if (isLoading) return <LoadingState message="Refining cluster insights..." />;
  if (error || !cluster) return <ErrorState message="Error loading cluster." onRetry={() => navigate(-1)} />;

  const handleGenerateSummary = async () => {
    // Check tier allowance before attempting
    if (billingStatus?.subscriptionTier === "Free") {
      setIsUpgradeModalOpen(true);
      return;
    }

    setIsGeneratingSummaryLocal(true);
    try {
      await api.post(`/clusters/${cluster.id}/generate-summary`, {});
      await queryClient.invalidateQueries({ queryKey: ["cluster", id] });
      await queryClient.invalidateQueries({ queryKey: ["billing-status"] });
    } catch (e: unknown) {
      console.error("Failed to generate summary", e);
      const axiosError = e as { response?: { status?: number } };
      if (axiosError.response?.status === 402 || axiosError.response?.status === 403) {
        setIsUpgradeModalOpen(true);
      } else {
        toast.error("Failed to generate summary");
      }
    } finally {
      setIsGeneratingSummaryLocal(false);
    }
  };

  const handleClearSummary = async () => {
    setIsClearingSummary(true);
    try {
      await api.post(`/clusters/${cluster.id}/clear-summary`, {});
      await queryClient.invalidateQueries({ queryKey: ["cluster", id] });
    } catch {
      console.error("Failed to clear summary");
      toast.error("Failed to clear summary history");
    } finally {
      setIsClearingSummary(false);
    }
  };

  const handleResolveCluster = async () => {
    try {
      await api.post(`/clusters/${cluster.id}/close-cluster`, {});
      await queryClient.invalidateQueries({ queryKey: ["cluster", id] });
      toast.success("Cluster resolved successfully");
    } catch {
      toast.error("Failed to resolve cluster");
    } finally {
      setIsResolveConfirmOpen(false);
    }
  };

  return (
    <PageState className="max-w-6xl">
      <div className="flex flex-col gap-8 pb-20">
        <ClusterHero 
          cluster={cluster} 
          onCloseCluster={() => setIsResolveConfirmOpen(true)} 
        />

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          <div className="lg:col-span-2 flex flex-col gap-8">
            <AiInsightCard
              cluster={cluster}
              isGeneratingSummary={!!isGeneratingSummary}
              isClearingSummary={isClearingSummary}
              onGenerateSummary={handleGenerateSummary}
              onClearSummary={handleClearSummary}
            />
          </div>

          <div className="flex flex-col gap-8">
            <ImpactedReportsList reports={cluster.reports} />
          </div>
        </div>
      </div>

      <ConfirmActionDialog
        open={isResolveConfirmOpen}
        onOpenChange={setIsResolveConfirmOpen}
        title="Resolve Cluster?"
        description={`This will mark all ${cluster.reportCount} reports in this cluster as Closed. This action cannot be undone.`}
        onConfirm={handleResolveCluster}
        actionText="Proceed"
      />

      <UpgradeRequiredModal
        open={isUpgradeModalOpen}
        onOpenChange={setIsUpgradeModalOpen}
      />
    </PageState>
  );
}
