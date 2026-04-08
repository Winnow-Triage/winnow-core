export interface User {
  id: string;
  email: string;
  fullName: string;
  isEmailVerified: boolean;
  roles: string[];
  permissions?: string[];
  activeOrganizationId?: string;
  defaultProjectId?: string;
  emailBounced?: boolean;
}

export interface NotificationSettings {
  volumeThreshold?: number | null;
  criticalityThreshold?: number | null;
}

export interface Project {
  id: string;
  name: string;
  apiKey: string;
  teamId?: string | null;
  hasSecondaryKey?: boolean;
  secondaryApiKeyExpiresAt?: string | null;
  notifications: NotificationSettings;
}

export interface Team {
  id: string;
  name: string;
}

export interface LoginResponse {
  userId: string;
  email: string;
  fullName: string;
  isEmailVerified: boolean;
  roles: string[];
  permissions?: string[];
  activeOrganizationId?: string;
  defaultProjectId?: string;
  emailBounced?: boolean;
}

export interface IntegrationConfig {
  id: string;
  provider: string;
  name: string;
  settingsJson: string;
  isActive: boolean;
  notificationsEnabled: boolean;
  isVerified?: boolean;
}

export interface ProjectIntegration extends IntegrationConfig {
  projectId: string;
}

export interface DashboardMetrics {
  totalReports: number;
  criticalReports: number;
  assignedToMe: number;
  trends: {
    labels: string[];
    data: number[];
  };
}

export interface AIProvider {
  name: string;
  type: string;
  providerId: string;
  provider: string;
  apiKey: string;
  modelId: string;
}

export interface ToxicityLimits {
  profanity: number;
  hateSpeech: number;
  violence: number;
  insult: number;
  harassment: number;
  sexual: number;
  graphic: number;
  overall: number;
}

export interface Organization {
  id: string;
  name: string;
  subscriptionTier: string;
  toxicityFilterEnabled: boolean;
  toxicityLimits: ToxicityLimits;
  aiConfig: {
    tokenizer: string;
    summaryAgent: string;
    customProviders: AIProvider[];
  };
  notifications: NotificationSettings;
}

export interface AIUsageBreakdown {
  model: string;
  provider: string;
  inputTokens: number;
  outputTokens: number;
  callCount: number;
}

export interface BillingStatus {
  reportsUsedThisMonth: number;
  reportLimit: number | null;
  currentMonthSummaries: number;
  monthlySummaryLimit: number | null;
  monthlyInputTokens: number;
  monthlyOutputTokens: number;
  aiUsageBreakdown: AIUsageBreakdown[];
}

export interface OrgMember {
  id: string;
  fullName: string | null;
  email: string;
  globalRole: string;
  roleId: string;
  status: string;
  isLocked: boolean;
  joinedAt?: string;
}

export interface TeamMember {
  userId: string;
  fullName: string;
}

export interface TeamDetail extends Team {
  createdAt: string;
  projectCount: number;
  members: TeamMember[];
  projects: { id: string; name: string }[];
}
