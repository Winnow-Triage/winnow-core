import type { AxiosInstance } from "axios";

/**
 * setupMocks initializes the "Truman Show" interceptor for the Axios client.
 * It intercepts ALL requests (GET, POST, PUT, DELETE, PATCH) and returns
 * realistic, static mock data when VITE_DEMO_MODE is true.
 */
export const setupMocks = (api: AxiosInstance) => {
  console.log("🛠️ Truman Show Mock Factory Initialized (Complete Mode)");

  api.interceptors.request.use(async (config) => {
    if (import.meta.env.VITE_DEMO_MODE !== "true") return config;

    // Simulate realistic network latency (300ms - 800ms)
    const latency = Math.floor(Math.random() * 500) + 300;
    await new Promise((resolve) => setTimeout(resolve, latency));

    const url = config.url || "";
    const method = config.method?.toLowerCase() || "get";

    // Helper to check if URL ends with or matches a path
    const matches = (path: string) => url === path || url.endsWith(path);

    // --- GENERIC MUTATION HANDLER ---
    if (method !== "get" && method !== "head") {
      let mockData = config.data;
      if (typeof mockData === "string") {
        try { mockData = JSON.parse(mockData); } catch (e) { /* ignore */ }
      }

      // Special handling for auth mutations to ensure the demo session can be simulated
      // Robust check for various URL formats (absolute vs relative)
      const isAuthUrl = url.includes("/auth/login") || 
                        url.includes("/auth/register") || 
                        url.includes("/auth/switch");

      if (isAuthUrl) {
        localStorage.setItem("demo_authenticated", "true");
        mockData = MOCK_USER_DTO;
      }
      
      if (url.includes("/auth/logout")) {
        localStorage.removeItem("demo_authenticated");
        mockData = { success: true };
      }

      config.adapter = async (mockConfig) => ({
        data: mockData || {},
        status: 200,
        statusText: "OK",
        headers: { "content-type": "application/json" },
        config: mockConfig,
      });
      return config;
    }

    // --- GET HANDLERS ---
    let responseData: any = null;

    // Core Auth & Profile
    if (matches("/auth/me") || matches("/account/me")) {
      if (localStorage.getItem("demo_authenticated") !== "true") {
        config.adapter = async () => {
          const error: any = new Error("Request failed with status code 401");
          error.isAxiosError = true;
          error.response = { status: 401, data: null, statusText: "Unauthorized" };
          return Promise.reject(error);
        };
        return config;
      }
      responseData = MOCK_USER_DTO;
    }
    // Organizations
    else if (matches("/organizations/current")) {
      responseData = MOCK_ORG_SETTINGS;
    } else if (matches("/organizations/current/members")) {
      responseData = MOCK_ORG_MEMBERS;
    } else if (url.includes("/roles") && url.includes("/organizations/")) {
      responseData = { roles: MOCK_ORG_ROLES };
    } else if (matches("/organizations")) {
      responseData = MOCK_ORGS;
    }
    // Projects
    else if (matches("/projects")) {
      responseData = MOCK_PROJECTS;
    } else if (url.includes("/integrations") && url.includes("/projects/")) {
      responseData = MOCK_INTEGRATIONS;
    }
    // Billing
    else if (matches("/billing/status")) {
      responseData = MOCK_BILLING;
    }
    // Dashboards (Handles both /dashboard/metrics and team-specific ones)
    else if (matches("/dashboard/metrics") || url.includes("/dashboard/organization/metrics")) {
      responseData = MOCK_DASHBOARD_METRICS;
    } else if (url.includes("/dashboard/teams/") && url.includes("/metrics")) {
      responseData = MOCK_TEAM_DASHBOARD_METRICS;
    }
    // Teams
    else if (matches("/teams")) {
      responseData = MOCK_TEAMS_LIST;
    }
    // Admin Panels
    else if (matches("/admin/organizations")) {
      responseData = MOCK_ADMIN_ORGS;
    } else if (url.match(/\/admin\/organizations\/[^\/]+$/)) {
      responseData = MOCK_ORG_DETAILS_ADMIN;
    } else if (matches("/admin/users")) {
      responseData = MOCK_ADMIN_USERS;
    }
    // Triage Data (Reports & Clusters)
    else if (matches("/reports/review-queue")) {
      responseData = MOCK_REVIEW_QUEUE;
    } else if (matches("/reports/search")) {
      responseData = generateMockReports(config.params);
    } else if (url.match(/\/reports\/[^\/]+$/)) {
      const id = url.split("/").pop()!;
      responseData = generateMockReportDetail(id);
    } else if (matches("/clusters/search")) {
      responseData = generateMockClusters(config.params);
    } else if (url.match(/\/clusters\/[^\/]+$/)) {
      const id = url.split("/").pop()!;
      responseData = generateMockClusterDetail(id);
    }
    // Health Check
    else if (matches("/health/detailed")) {
      responseData = MOCK_HEALTH;
    }

    if (responseData) {
      config.adapter = async (mockConfig) => ({
        data: responseData,
        status: 200,
        statusText: "OK",
        headers: { "content-type": "application/json" },
        config: mockConfig,
      });
    }

    return config;
  });
};

// --- MOCK DATA DEFINITIONS ---

const MOCK_USER_DTO = {
  id: "demo-user-123",
  userId: "demo-user-123",
  email: "demo@winnowtriage.com",
  fullName: "Truman Burbank",
  isEmailVerified: true,
  roles: ["admin", "owner"],
  permissions: ["*", "projects:manage", "organizations:manage", "billing:manage"],
  activeOrganizationId: "demo-org-alpha",
  defaultProjectId: "demo-proj-main",
};

const MOCK_ORG_SETTINGS = {
  id: "demo-org-alpha",
  name: "Winnow Demo Corp",
  slug: "winnow-demo-corp",
  aiConfig: {
    tokenizer: "Default",
    summaryAgent: "Default",
    customProviders: [],
  },
};

const MOCK_ORGS = [
  MOCK_ORG_SETTINGS,
  { id: "org-beta", name: "Acme Cloud Services" },
];

const MOCK_PROJECTS = [
  { id: "demo-proj-main", name: "Core API", apiKey: "win_live_12345", teamId: "team-eng" },
  { id: "demo-proj-ui", name: "Web Dashboard", apiKey: "win_live_67890", teamId: "team-eng" },
];

const MOCK_INTEGRATIONS = [
  { id: "int-1", type: "GitHub", provider: "GitHub", status: "Connected", name: "winnow-secure/winnow-core" },
  { id: "int-2", type: "Slack", provider: "Slack", status: "Connected", name: "#winnow-alerts" },
];

const MOCK_BILLING = {
  subscriptionTier: "Pro",
  reportsUsedThisMonth: 12450,
  reportLimit: 50000,
  monthlySummaryLimit: 1000,
  currentMonthSummaries: 412,
  hasActiveSubscription: true,
  monthlyInputTokens: 15420000,
  monthlyOutputTokens: 2500000,
  aiUsageBreakdown: [
    { model: "gpt-4o", provider: "openai", inputTokens: 10000000, outputTokens: 1500000, callCount: 5000 },
    { model: "claude-3-sonnet", provider: "anthropic", inputTokens: 5420000, outputTokens: 1000000, callCount: 3500 },
  ],
};

function generateVolumeHistory() {
  const history = [];
  const now = new Date();
  for (let i = 24; i >= 0; i--) {
    const d = new Date(now.getTime() - i * 60 * 60 * 1000);
    const hour = d.getHours();
    let unique = Math.floor(Math.random() * 40) + 10;
    let duplicates = Math.floor(Math.random() * 120) + 40;
    if (hour >= 1 && hour <= 6) {
      if (Math.random() > 0.4) { unique = 0; duplicates = 0; }
      else { unique = Math.floor(Math.random() * 5); duplicates = Math.floor(Math.random() * 10); }
    }
    if (hour >= 10 && hour <= 16) { unique += 30; duplicates += 60; }
    history.push({
      timestamp: d.toISOString(),
      newUniqueCount: unique,
      duplicateCount: duplicates,
    });
  }
  return history;
}

const MOCK_VOLUME_HISTORY = generateVolumeHistory();

const MOCK_DASHBOARD_METRICS = {
  triage: {
    totalReports: 12450,
    activeClusters: 85,
    noiseReductionRatio: 0.924,
    pendingReviews: 14,
    estimatedHoursSaved: 156,
  },
  trendingClusters: [
    { clusterId: "c-1", title: "Authentication Failures", status: "Critical", reportCount: 452, velocity: 82, isHot: true },
    { clusterId: "c-2", title: "Database Timeouts", status: "High", reportCount: 1240, velocity: 45, isHot: false },
    { clusterId: "c-3", title: "Memory Management Issues", status: "Medium", reportCount: 215, velocity: 12, isHot: false },
  ],
  volumeHistory: MOCK_VOLUME_HISTORY,
  quota: {
    totalUsage: 12450,
    baseLimit: 50000,
    graceLimit: 5000,
    isOverage: false,
    usageHistory: [
      { month: "Jan", reportCount: 8500, clusterCount: 120 },
      { month: "Feb", reportCount: 9200, clusterCount: 140 },
      { month: "Mar", reportCount: 12450, clusterCount: 185 },
    ],
  },
  teamBreakdown: [
    { teamId: "team-eng", teamName: "Engineering", projectCount: 4, reportVolume: 8500 },
    { teamId: "team-qa", teamName: "Quality Assurance", projectCount: 2, reportVolume: 3950 },
  ],
  topProjects: [
    { projectId: "demo-proj-main", projectName: "Core API", reportCount: 4500, activeClusters: 42 },
    { projectId: "demo-proj-ui", projectName: "Web Dashboard", reportCount: 3200, activeClusters: 28 },
  ],
};

const MOCK_TEAM_DASHBOARD_METRICS = {
  ...MOCK_DASHBOARD_METRICS,
  projectBreakdown: [
    { projectId: "demo-proj-main", projectName: "Core API", reportVolume: 4500, activeClusters: 42 },
    { projectId: "demo-proj-ui", projectName: "Web Dashboard", reportVolume: 3200, activeClusters: 28 },
  ],
  topClusters: MOCK_DASHBOARD_METRICS.trendingClusters,
};

const MOCK_TEAMS_LIST = [
  { 
    id: "team-eng", 
    name: "Engineering", 
    createdAt: "2023-01-15T10:00:00Z", 
    projectCount: 4,
    members: [{ userId: "demo-user-123", fullName: "Truman Burbank" }, { userId: "u-2", fullName: "Marlon Macready" }],
    projects: MOCK_PROJECTS
  },
  { 
    id: "team-qa", 
    name: "Quality Assurance", 
    createdAt: "2023-02-20T11:00:00Z", 
    projectCount: 2,
    members: [{ userId: "demo-user-123", fullName: "Truman Burbank" }],
    projects: [MOCK_PROJECTS[1]]
  },
];

const MOCK_ORG_MEMBERS = [
  { id: "demo-user-123", fullName: "Truman Burbank", email: "demo@winnow.app", globalRole: "owner", roleId: "role-owner", status: "Active", isLocked: false, joinedAt: "2023-01-01T00:00:00Z" },
  { id: "u-2", fullName: "Marlon Macready", email: "marlon@winnow.app", globalRole: "admin", roleId: "role-admin", status: "Active", isLocked: false, joinedAt: "2023-01-15T00:00:00Z" },
  { id: "u-3", fullName: "Christof", email: "director@winnow.app", globalRole: "member", roleId: "role-member", status: "Active", isLocked: true, joinedAt: "2023-01-20T00:00:00Z" },
  { id: "u-4", fullName: null, email: "new-hire@winnow.app", globalRole: "member", roleId: "role-member", status: "Pending", isLocked: false },
];

const MOCK_ORG_ROLES = [
  { id: "role-owner", name: "Owner" },
  { id: "role-admin", name: "Admin" },
  { id: "role-member", name: "Member" },
];

const MOCK_ADMIN_ORGS = [
  { id: "demo-org-alpha", name: "Winnow Demo Corp", stripeCustomerId: "cus_123", subscriptionTier: "Enterprise", createdAt: "2023-01-01T00:00:00Z", isSuspended: false, teamCount: 5, memberCount: 24, projectCount: 12 },
  { id: "org-beta", name: "Acme Cloud Services", stripeCustomerId: "cus_456", subscriptionTier: "Pro", createdAt: "2023-06-15T00:00:00Z", isSuspended: false, teamCount: 2, memberCount: 8, projectCount: 4 },
];

const MOCK_ORG_DETAILS_ADMIN = {
  ...MOCK_ADMIN_ORGS[0],
  isPaidTier: true,
  reportCount: 85400,
  assetCount: 1200,
  integrationCount: 8,
  lastReportDate: "2024-03-25T08:15:00Z",
  lastMemberJoinDate: "2024-03-20T14:30:00Z",
  teams: MOCK_TEAMS_LIST,
  members: MOCK_ORG_MEMBERS,
  quota: MOCK_DASHBOARD_METRICS.quota,
  projectQuotas: [
    { id: "demo-proj-main", name: "Core API", monthlyReportCount: 4500 },
    { id: "demo-proj-ui", name: "Web Dashboard", monthlyReportCount: 3200 },
  ],
};

const MOCK_ADMIN_USERS = [
  { id: "demo-user-123", email: "demo@winnow.app", fullName: "Truman Burbank", roles: ["admin"], createdAt: "2023-01-01T00:00:00Z", isLockedOut: false, organizations: [{ id: "demo-org-alpha", name: "Winnow Demo Corp" }] },
  { id: "u-2", email: "marlon@winnow.app", fullName: "Marlon Macready", roles: ["user"], createdAt: "2023-01-15T00:00:00Z", isLockedOut: false, organizations: [{ id: "demo-org-alpha", name: "Winnow Demo Corp" }] },
];

const MOCK_HEALTH = {
  status: "Healthy",
  totalDuration: "145ms",
  utcTimestamp: "2024-03-25T08:20:00Z",
  checks: [
    { name: "PostgreSQL", status: "Healthy", duration: "12ms" },
    { name: "Redis Cache", status: "Healthy", duration: "5ms" },
    { name: "S3 Storage", status: "Healthy", duration: "85ms" },
    { name: "AI Gateway", status: "Healthy", duration: "43ms" },
  ],
};

function generateMockReports(params: any) {
  const query = params?.q || params?.query || params?.search || "";
  const items = [
    { id: "rep-1", title: "Unhandled Exception: InvalidOperationException", description: "Sequence contains no elements at AuthProvider.GetSession.", status: "Open", createdAt: "2024-03-25T08:00:00Z", updatedAt: "2024-03-25T08:00:00Z", clusterId: "c-1", criticalityScore: 10, projectId: "demo-proj-main" },
    { id: "rep-2", title: "Connection Timeout: Database Cluster Alpha", description: "Failed to connect to postgres-replica-01 after 5000ms.", status: "Open", createdAt: "2024-03-25T07:45:00Z", updatedAt: "2024-03-25T07:45:00Z", clusterId: "c-2", criticalityScore: 9, projectId: "demo-proj-main" },
    { id: "rep-3", title: "Memory Leak Detected in Dashboard Session", description: "Allocated heap growth +450MB over 4 hours.", status: "Investigating", createdAt: "2024-03-25T07:30:00Z", updatedAt: "2024-03-25T07:30:00Z", clusterId: "c-3", criticalityScore: 7, projectId: "demo-proj-ui" },
    { id: "rep-4", title: "Rate Limit Exceeded: OpenAI GPT-4 API", description: "429 Too Many Requests in ClusterRefinementJob.", status: "Resolved", createdAt: "2024-03-25T06:00:00Z", updatedAt: "2024-03-25T06:00:00Z", criticalityScore: 4, projectId: "demo-proj-main" },
    { id: "rep-5", title: "Missing Metadata in S3 Upload", description: "File key abc-123 failed EXIF scrub processing.", status: "Open", createdAt: "2024-03-25T05:30:00Z", updatedAt: "2024-03-25T05:30:00Z", criticalityScore: 3, projectId: "demo-proj-main" },
  ].map(item => {
    // Basic relevancy simulation for demo purposes
    let score = query ? (0.01 + Math.random() * 0.05) : undefined;
    if (query && score !== undefined) {
      const q = query.toLowerCase();
      if (item.title.toLowerCase().includes(q)) score += 0.04;
      if (item.description.toLowerCase().includes(q)) score += 0.02;
    }
    return { ...item, relevanceScore: score };
  });

  if (query) {
    items.sort((a, b) => (b.relevanceScore || 0) - (a.relevanceScore || 0));
  }
  
  return { items, totalCount: items.length, pageNumber: 1, pageSize: 20 };
}

const MOCK_REVIEW_QUEUE = [
  { sourceId: "rep-6", sourceTitle: "Redis Connection Refused", sourceMessage: "Could not connect to Redis at 127.0.0.1:6379", sourceStackTrace: "at Redis.Client.Connect()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T04:00:00Z", targetId: "c-2", targetTitle: "Database Timeouts", targetSummary: "Intermittent timeouts on read replicas during peak traffic hours.", confidenceScore: 0.94, type: "Report" },
  { sourceId: "rep-7", sourceTitle: "Slow Query: SELECT * FROM reports", sourceMessage: "Query took 12.5s to execute.", sourceStackTrace: "at DB.Query.Execute()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T03:45:00Z", targetId: "c-2", targetTitle: "Database Timeouts", targetSummary: "Intermittent timeouts on read replicas during peak traffic hours.", confidenceScore: 0.88, type: "Report" },
  { sourceId: "rep-8", sourceTitle: "Illegal State Exception in TriageJob", sourceMessage: "Job expected state 'Active' but found 'Pending'.", sourceStackTrace: "at Triage.Job.Run()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T03:30:00Z", targetId: "c-1", targetTitle: "Authentication Failures", targetSummary: "Multiple failures in auth middleware.", confidenceScore: 0.72, type: "Report" },
  { sourceId: "rep-9", sourceTitle: "File Not Found in S3", sourceMessage: "Key 'uploads/temp/x.y' missing.", sourceStackTrace: "at S3.Client.Get()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T03:15:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.44, type: "Report" },
  { sourceId: "rep-10", sourceTitle: "Auth Token Expired Paradoxically", sourceMessage: "Token marked expired 5 minutes before generation time.", sourceStackTrace: "at Auth.Token.Verify()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T03:00:00Z", targetId: "c-1", targetTitle: "Authentication Failures", targetSummary: "Multiple failures in auth middleware.", confidenceScore: 0.96, type: "Report" },
  { sourceId: "rep-11", sourceTitle: "CSS Layout Shift on Landing Page", sourceMessage: "Detected 0.45 CLS on mobile views.", sourceStackTrace: null, sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T02:45:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.22, type: "Report" },
  { sourceId: "rep-12", sourceTitle: "Broken Link in Welcome Email", sourceMessage: "The 'Get Started' button points to a 404 page.", sourceStackTrace: null, sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T02:30:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.35, type: "Report" },
  { sourceId: "rep-13", sourceTitle: "Stripe Webhook Verification Failed", sourceMessage: "Signature verification failed.", sourceStackTrace: "at Stripe.Client.Verify()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T02:15:00Z", targetId: "c-1", targetTitle: "Authentication Failures", targetSummary: "Multiple failures in auth middleware.", confidenceScore: 0.78, type: "Report" },
  { sourceId: "rep-14", sourceTitle: "Markdown Rendering Error", sourceMessage: "Unescaped characters causing break.", sourceStackTrace: null, sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T02:00:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.15, type: "Report" },
  { sourceId: "c-3", sourceTitle: "Memory Management Issues", sourceMessage: "High memory usage reports from frontend dashboard clients.", sourceStackTrace: null, sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-23T14:00:00Z", targetId: "c-1", targetTitle: "Authentication Failures", targetSummary: "Multiple failures in auth middleware.", confidenceScore: 0.12, type: "Cluster" },
  { sourceId: "rep-15", sourceTitle: "Null Reference in Layout", sourceMessage: "Object not set to an instance of an object at Layout.Render", sourceStackTrace: "at Layout.Render()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T01:45:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.82, type: "Report" },
  { sourceId: "rep-16", sourceTitle: "Invalid Operation in Cache", sourceMessage: "Collection was modified; enumeration operation may not execute.", sourceStackTrace: "at Cache.Enumerator.MoveNext()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T01:30:00Z", targetId: "c-3", targetTitle: "Memory Management Issues", targetSummary: "High memory usage reports.", confidenceScore: 0.65, type: "Report" },
  { sourceId: "rep-17", sourceTitle: "Timeout in Search API", sourceMessage: "The operation has timed out after 30s.", sourceStackTrace: "at Search.API.Call()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T01:15:00Z", targetId: "c-2", targetTitle: "Database Timeouts", targetSummary: "Intermittent timeouts on read replicas during peak traffic hours.", confidenceScore: 0.91, type: "Report" },
  { sourceId: "rep-18", sourceTitle: "Access Denied to Assets", sourceMessage: "The user does not have permission to view asset xyz.", sourceStackTrace: "at Asset.Provider.Authorize()", sourceAssignedTo: "Truman Burbank", sourceCreatedAt: "2024-03-25T01:00:00Z", targetId: "c-1", targetTitle: "Authentication Failures", targetSummary: "Multiple failures in auth middleware.", confidenceScore: 0.85, type: "Report" },
];

function generateMockClusters(params: any) {
  const query = params?.q || params?.query || params?.search || "";
  const items = [
    { id: "c-1", title: "Authentication Failures", summary: "Multiple NullReference and InvalidOperation exceptions in the auth middleware.", status: "Active", createdAt: "2024-03-21T10:00:00Z", criticalityScore: 9, reportCount: 452, isLocked: false, isOverage: false, isSummarizing: false, projectId: "demo-proj-main", velocity1h: 5, velocity24h: 42, firstSeen: "2024-03-21T10:00:00Z", lastSeen: "2024-03-25T08:00:00Z" },
    { id: "c-2", title: "Database Timeouts", summary: "Intermittent timeouts on read replicas during peak traffic hours.", status: "Active", createdAt: "2024-03-22T12:00:00Z", criticalityScore: 8, reportCount: 1240, isLocked: false, isOverage: false, isSummarizing: false, projectId: "demo-proj-main", velocity1h: 2, velocity24h: 120, firstSeen: "2024-03-22T12:00:00Z", lastSeen: "2024-03-25T07:45:00Z" },
    { id: "c-3", title: "Memory Management Issues", summary: "High memory usage reports from frontend dashboard clients.", status: "Triage", createdAt: "2024-03-23T14:00:00Z", criticalityScore: 6, reportCount: 215, isLocked: false, isOverage: false, isSummarizing: false, projectId: "demo-proj-ui", velocity1h: 0, velocity24h: 15, firstSeen: "2024-03-23T14:00:00Z", lastSeen: "2024-03-25T07:30:00Z" },
  ].map(item => {
    // Basic relevancy simulation
    let score = query ? (0.01 + Math.random() * 0.05) : undefined;
    if (query && score !== undefined) {
      const q = query.toLowerCase();
      if (item.title.toLowerCase().includes(q)) score += 0.04;
      if (item.summary.toLowerCase().includes(q)) score += 0.02;
    }
    return { ...item, relevanceScore: score };
  });

  if (query) {
    items.sort((a, b) => (b.relevanceScore || 0) - (a.relevanceScore || 0));
  }
  
  return { items, totalCount: items.length, pageNumber: 1, pageSize: 20 };
}

function generateMockReportDetail(id: string) {
  const base = generateMockReports({}).items.find(r => r.id === id) || generateMockReports({}).items[0];
  return {
    ...base,
    message: (base as any).message || base.description,
    stackTrace: `Winnow.Auth.Provider.GetSession(String token) in AuthProvider.cs:line 42\nWinnow.Auth.Middleware.Invoke(HttpContext context) in Middleware.cs:line 104\nSystem.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)`,
    fullDescription: `${base.description}\n\nThis issue appears to be caused by an unexpected null token being passed to the auth provider.`,
    metadata: JSON.stringify({
      version: "1.2.4",
      environment: "production",
      browser: "Chrome 122.0.0",
      os: "macOS 14.3.1",
    }, null, 2),
    assets: [
      { id: "asset-1", fileName: "screenshot.png", contentType: "image/png", sizeBytes: 1024500, status: "Clean", createdAt: base.createdAt },
      { id: "asset-2", fileName: "logs.txt", contentType: "text/plain", sizeBytes: 54200, status: "Clean", createdAt: base.createdAt },
    ],
    evidence: [
      { id: "ev-1", message: "Similar auth failure in staging", status: "Closed", createdAt: "2024-03-24T10:00:00Z", confidenceScore: 0.85 }
    ],
    criticalityReasoning: "Authentication failures prevent users from accessing the system, directly impacting business continuity."
  };
}

function generateMockClusterDetail(id: string) {
  const base = generateMockClusters({}).items.find(c => c.id === id) || generateMockClusters({}).items[0];
  return {
    ...base,
    criticalityReasoning: "This cluster aggregates critical failures in the core authentication flow, which is the highest risk area for the application.",
    reports: generateMockReports({}).items.map(r => ({ ...r, message: r.description, confidenceScore: 0.8 + Math.random() * 0.15 })),
    similarClusters: [],
  };
}
