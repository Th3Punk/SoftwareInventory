export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApplicationListItem {
  id: number;
  name: string;
  shortDescription: string;
  status: string;
  type: string;
  ownerTeam: string;
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

export interface ApplicationDetail {
  id: number;
  name: string;
  shortDescription: string;
  detailedDescription: string | null;
  status: string;
  type: string;
  ownerTeam: string;
  sourceControl: string;
  repositoryUrl: string | null;
  wikiUrl: string | null;
  tags: string[];
  environments: Environment[];
  contacts: Contact[];
  createdByUserId: number | null;
  createdByName: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Environment {
  id: number;
  type: string;
  url: string;
  notes: string | null;
  isPublic: boolean;
}

export interface Contact {
  id: number;
  type: string;
  value: string;
  label: string | null;
}

export interface Tag {
  id: number;
  name: string;
  color: string | null;
}

export interface FeatureFlags {
  [key: string]: {
    enabled: boolean;
    [key: string]: unknown;
  };
}

export interface ApplicationFilters {
  status?: string;
  type?: string;
  team?: string;
  tags?: string[];
  q?: string;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export interface SearchResultItem {
  resourceType: string;
  resourceId: number;
  title: string;
  snippet: string;
  score: number;
}

export interface SearchResponse {
  items: SearchResultItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SearchFilters {
  q: string;
  type?: string[];
  tag?: string[];
  page?: number;
  pageSize?: number;
}

export interface DocumentationListItem {
  id: number;
  title: string;
  type: string;
  status: string;
  version: number;
  updatedAt: string;
}

export interface DocumentationDetail {
  id: number;
  title: string;
  content: string;
  type: string;
  status: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  authorName: string | null;
}

export interface CreateDocumentationRequest {
  title: string;
  content: string;
  type: string;
}

export interface UpdateDocumentationRequest {
  title: string;
  content: string;
  type: string;
}

export interface AuditLog {
  id: number;
  userId: number | null;
  action: string;
  resourceType: string;
  resourceId: string;
  oldValueJson: string | null;
  newValueJson: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface AuditLogDetail extends AuditLog {
  userAgent: string | null;
}

export interface AuditLogFilters {
  userId?: number;
  resourceType?: string;
  resourceId?: string;
  action?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export interface AdminUser {
  id: number;
  displayName: string;
  email: string;
  isActive: boolean;
  lastLogin: string | null;
  createdAt: string;
  roles: UserRoleSummary[];
}

export interface UserRoleSummary {
  roleId: number;
  roleName: string;
  source: string;
  grantedAt: string;
}

export interface AdminUserFilters {
  isActive?: boolean;
  q?: string;
  page?: number;
  pageSize?: number;
}

export interface GroupRoleMapping {
  id: number;
  providerType: string;
  externalGroupRef: string;
  roleId: number;
  roleName: string;
  description: string | null;
  isActive: boolean;
}

export interface Role {
  id: number;
  name: string;
  description: string | null;
  isSystemRole: boolean;
}

export interface CreateGroupRoleMappingRequest {
  providerType: string;
  externalGroupRef: string;
  roleId: number;
  description?: string;
}

export interface UpdateGroupRoleMappingRequest {
  description?: string;
  isActive: boolean;
}

export interface CurrentUser {
  id: number;
  displayName: string;
  email: string;
  roles: string[];
  mustChangePassword: boolean;
}
