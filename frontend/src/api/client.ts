import type {
  ApplicationDetail,
  ApplicationFilters,
  ApplicationListItem,
  CreateDocumentationRequest,
  DocumentationDetail,
  DocumentationListItem,
  FeatureFlags,
  PagedResponse,
  SearchFilters,
  SearchResponse,
  Tag,
  UpdateDocumentationRequest,
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
