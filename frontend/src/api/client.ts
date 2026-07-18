import type {
  AdminUser,
  AdminUserFilters,
  ApplicationDetail,
  ApplicationFilters,
  ApplicationListItem,
  AuditLog,
  AuditLogDetail,
  AuditLogFilters,
  CreateDocumentationRequest,
  CreateGroupRoleMappingRequest,
  CurrentUser,
  DocumentationDetail,
  DocumentationListItem,
  FeatureFlags,
  GroupRoleMapping,
  PagedResponse,
  Role,
  SearchFilters,
  SearchResponse,
  Tag,
  UpdateDocumentationRequest,
  UpdateGroupRoleMappingRequest,
} from "./types";

const BASE_URL = "/api/v1";

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    ...options,
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(response.status, body?.detail ?? response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export async function fetchApplications(
  filters: ApplicationFilters = {},
): Promise<PagedResponse<ApplicationListItem>> {
  const params = new URLSearchParams();

  if (filters.status) params.set("status", filters.status);
  if (filters.type) params.set("type", filters.type);
  if (filters.team) params.set("team", filters.team);
  if (filters.q) params.set("q", filters.q);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  if (filters.sort) params.set("sort", filters.sort);
  if (filters.tags) {
    for (const tag of filters.tags) {
      params.append("tag", tag);
    }
  }

  const qs = params.toString();
  return request(`/applications${qs ? `?${qs}` : ""}`);
}

export async function fetchApplication(id: number): Promise<ApplicationDetail> {
  return request(`/applications/${id}`);
}

export async function fetchTags(): Promise<Tag[]> {
  return request("/tags");
}

export async function createTag(name: string, color: string | null): Promise<Tag> {
  return request("/tags", {
    method: "POST",
    body: JSON.stringify({ name, color }),
  });
}

export async function deleteTag(id: number): Promise<void> {
  return request(`/tags/${id}`, { method: "DELETE" });
}

export async function search(filters: SearchFilters): Promise<SearchResponse> {
  const params = new URLSearchParams();
  params.set("q", filters.q);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  if (filters.type) {
    for (const t of filters.type) params.append("type", t);
  }
  if (filters.tag) {
    for (const t of filters.tag) params.append("tag", t);
  }
  return request(`/search?${params.toString()}`);
}

export async function fetchDocumentations(appId: number): Promise<DocumentationListItem[]> {
  return request(`/applications/${appId}/docs`);
}

export async function fetchDocumentation(
  appId: number,
  docId: number,
): Promise<DocumentationDetail> {
  return request(`/applications/${appId}/docs/${docId}`);
}

export async function createDocumentation(
  appId: number,
  req: CreateDocumentationRequest,
): Promise<DocumentationDetail> {
  return request(`/applications/${appId}/docs`, {
    method: "POST",
    body: JSON.stringify(req),
  });
}

export async function updateDocumentation(
  appId: number,
  docId: number,
  req: UpdateDocumentationRequest,
): Promise<DocumentationDetail> {
  return request(`/applications/${appId}/docs/${docId}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });
}

export async function fetchAuditLogs(
  filters: AuditLogFilters = {},
): Promise<PagedResponse<AuditLog>> {
  const params = new URLSearchParams();
  if (filters.userId !== undefined) params.set("userId", String(filters.userId));
  if (filters.resourceType) params.set("resourceType", filters.resourceType);
  if (filters.resourceId) params.set("resourceId", filters.resourceId);
  if (filters.action) params.set("action", filters.action);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  const qs = params.toString();
  return request(`/admin/audit-logs${qs ? `?${qs}` : ""}`);
}

export async function fetchAuditLog(id: number): Promise<AuditLogDetail> {
  return request(`/admin/audit-logs/${id}`);
}

export async function fetchAdminUsers(
  filters: AdminUserFilters = {},
): Promise<PagedResponse<AdminUser>> {
  const params = new URLSearchParams();
  if (filters.isActive !== undefined) params.set("isActive", String(filters.isActive));
  if (filters.q) params.set("q", filters.q);
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  const qs = params.toString();
  return request(`/admin/users${qs ? `?${qs}` : ""}`);
}

export async function setUserActive(id: number, isActive: boolean): Promise<void> {
  return request(`/admin/users/${id}/active`, {
    method: "PATCH",
    body: JSON.stringify({ isActive }),
  });
}

export async function assignUserRole(id: number, roleId: number): Promise<void> {
  return request(`/admin/users/${id}/roles`, {
    method: "POST",
    body: JSON.stringify({ roleId }),
  });
}

export async function removeUserRole(id: number, roleId: number): Promise<void> {
  return request(`/admin/users/${id}/roles/${roleId}`, { method: "DELETE" });
}

export async function fetchRoles(): Promise<Role[]> {
  return request("/admin/roles");
}

export async function fetchGroupRoleMappings(providerType?: string): Promise<GroupRoleMapping[]> {
  const qs = providerType ? `?providerType=${encodeURIComponent(providerType)}` : "";
  return request(`/admin/group-role-mappings${qs}`);
}

export async function createGroupRoleMapping(
  req: CreateGroupRoleMappingRequest,
): Promise<GroupRoleMapping> {
  return request("/admin/group-role-mappings", {
    method: "POST",
    body: JSON.stringify(req),
  });
}

export async function updateGroupRoleMapping(
  id: number,
  req: UpdateGroupRoleMappingRequest,
): Promise<GroupRoleMapping> {
  return request(`/admin/group-role-mappings/${id}`, {
    method: "PUT",
    body: JSON.stringify(req),
  });
}

export async function deleteGroupRoleMapping(id: number): Promise<void> {
  return request(`/admin/group-role-mappings/${id}`, { method: "DELETE" });
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return request("/auth/change-password", {
    method: "POST",
    body: JSON.stringify({ currentPassword, newPassword }),
  });
}

export async function login(username: string, password: string): Promise<CurrentUser> {
  return request("/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password }),
  });
}

export async function logout(): Promise<void> {
  return request("/auth/logout", { method: "POST" });
}

export async function fetchCurrentUser(): Promise<CurrentUser> {
  return request("/auth/me");
}

export async function fetchFeatureFlags(): Promise<FeatureFlags> {
  const response = await fetch(`${BASE_URL}/config/features`, {
    credentials: "include",
    headers: { "Content-Type": "application/json" },
  });

  if (!response.ok) {
    return {};
  }

  return response.json();
}
