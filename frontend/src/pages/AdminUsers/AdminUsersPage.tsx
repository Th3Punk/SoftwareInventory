import { useState, useEffect, useCallback } from "react";
import {
  fetchAdminUsers,
  setUserActive,
  assignUserRole,
  removeUserRole,
  fetchRoles,
} from "../../api/client";
import type { AdminUser, AdminUserFilters, Role } from "../../api/types";
import type { PagedResponse } from "../../api/types";
import "./AdminUsersPage.css";

export function AdminUsersPage() {
  const [q, setQ] = useState("");
  const [activeFilter, setActiveFilter] = useState<"" | "true" | "false">("");
  const [page, setPage] = useState(1);

  const [data, setData] = useState<PagedResponse<AdminUser> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [roles, setRoles] = useState<Role[]>([]);
  const [managingUserId, setManagingUserId] = useState<number | null>(null);
  const [assignRoleId, setAssignRoleId] = useState("");
  const [assigning, setAssigning] = useState(false);

  useEffect(() => {
    fetchRoles()
      .catch(() => {})
      .then((r) => {
        if (r) setRoles(r);
      });
  }, []);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    const filters: AdminUserFilters = {
      page,
      pageSize: 20,
      q: q || undefined,
      isActive: activeFilter === "" ? undefined : activeFilter === "true",
    };
    fetchAdminUsers(filters)
      .then((res) => setData(res))
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [page, q, activeFilter]);

  useEffect(() => {
    load();
  }, [load]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
  };

  const handleToggleActive = async (user: AdminUser) => {
    try {
      await setUserActive(user.id, !user.isActive);
      load();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to update");
    }
  };

  const handleAssignRole = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!managingUser || !assignRoleId) return;
    setAssigning(true);
    try {
      await assignUserRole(managingUser.id, Number(assignRoleId));
      setAssignRoleId("");
      load();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to assign role");
    } finally {
      setAssigning(false);
    }
  };

  const handleRemoveRole = async (userId: number, roleId: number) => {
    if (!confirm("Remove this role from the user?")) return;
    try {
      await removeUserRole(userId, roleId);
      load();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to remove role");
    }
  };

  const totalPages = data ? Math.ceil(data.totalCount / (data.pageSize || 20)) : 1;

  const managingUser =
    managingUserId != null ? (data?.items.find((u) => u.id === managingUserId) ?? null) : null;

  const assignableRoles = managingUser
    ? roles.filter((r) => !managingUser.roles.some((ur) => ur.roleId === r.id))
    : roles;

  return (
    <div className="admin-users-page">
      <h1>User Management</h1>

      <form className="admin-users-page__filters" onSubmit={handleSearch}>
        <input
          className="admin-users-page__input"
          placeholder="Search by name or email"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
        <select
          className="admin-users-page__input"
          value={activeFilter}
          onChange={(e) => setActiveFilter(e.target.value as "" | "true" | "false")}
        >
          <option value="">All users</option>
          <option value="true">Active only</option>
          <option value="false">Inactive only</option>
        </select>
        <button type="submit" className="admin-users-page__btn">
          Search
        </button>
      </form>

      {loading && <p className="admin-users-page__status">Loading...</p>}
      {error && <p className="admin-users-page__error">{error}</p>}

      {data && !loading && (
        <>
          <p className="admin-users-page__count">{data.totalCount} users</p>
          <div className="admin-users-page__table-wrapper">
            <table className="admin-users-page__table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Status</th>
                  <th>Last Login</th>
                  <th>Roles</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="admin-users-page__empty">
                      No users found.
                    </td>
                  </tr>
                )}
                {data.items.map((user) => (
                  <tr key={user.id} className="admin-users-page__row">
                    <td>{user.displayName}</td>
                    <td>{user.email}</td>
                    <td>
                      <span
                        className={`admin-users-page__status-badge ${user.isActive ? "admin-users-page__status-badge--active" : "admin-users-page__status-badge--inactive"}`}
                      >
                        {user.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td>{user.lastLogin ? new Date(user.lastLogin).toLocaleString() : "—"}</td>
                    <td>
                      <div className="admin-users-page__roles">
                        {user.roles.map((r) => (
                          <span key={r.roleId} className="admin-users-page__role-badge">
                            {r.roleName}
                            <button
                              className="admin-users-page__role-remove"
                              onClick={() => handleRemoveRole(user.id, r.roleId)}
                              title={`Remove role ${r.roleName}`}
                            >
                              ×
                            </button>
                          </span>
                        ))}
                        {user.roles.length === 0 && (
                          <span className="admin-users-page__no-roles">No roles</span>
                        )}
                      </div>
                    </td>
                    <td>
                      <div className="admin-users-page__actions">
                        <button
                          className={`admin-users-page__btn admin-users-page__btn--sm ${user.isActive ? "admin-users-page__btn--danger" : ""}`}
                          onClick={() => handleToggleActive(user)}
                        >
                          {user.isActive ? "Deactivate" : "Activate"}
                        </button>
                        <button
                          className="admin-users-page__btn admin-users-page__btn--sm admin-users-page__btn--secondary"
                          onClick={() => {
                            setManagingUserId(user.id);
                            setAssignRoleId("");
                          }}
                        >
                          Manage Roles
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="admin-users-page__pagination">
            <button
              className="admin-users-page__btn"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>
            <span>
              Page {page} of {totalPages}
            </span>
            <button
              className="admin-users-page__btn"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}

      {managingUser && (
        <div className="admin-users-page__modal-overlay" onClick={() => setManagingUserId(null)}>
          <div className="admin-users-page__modal" onClick={(e) => e.stopPropagation()}>
            <h2>Manage Roles — {managingUser.displayName}</h2>

            <div className="admin-users-page__current-roles">
              <h3>Current Roles</h3>
              {managingUser.roles.length === 0 && (
                <p className="admin-users-page__no-roles">No roles assigned.</p>
              )}
              <ul className="admin-users-page__role-list">
                {managingUser.roles.map((r) => (
                  <li key={r.roleId} className="admin-users-page__role-item">
                    <span>{r.roleName}</span>
                    <span className="admin-users-page__role-source">({r.source})</span>
                    <button
                      className="admin-users-page__btn admin-users-page__btn--sm admin-users-page__btn--danger"
                      onClick={() => handleRemoveRole(managingUser.id, r.roleId)}
                    >
                      Remove
                    </button>
                  </li>
                ))}
              </ul>
            </div>

            <form className="admin-users-page__assign-form" onSubmit={handleAssignRole}>
              <h3>Assign Role</h3>
              <select
                className="admin-users-page__input"
                value={assignRoleId}
                onChange={(e) => setAssignRoleId(e.target.value)}
                required
              >
                <option value="">Select role...</option>
                {assignableRoles.map((r) => (
                  <option key={r.id} value={String(r.id)}>
                    {r.name}
                    {r.description ? ` — ${r.description}` : ""}
                  </option>
                ))}
              </select>
              <button
                type="submit"
                className="admin-users-page__btn"
                disabled={assigning || !assignRoleId}
              >
                {assigning ? "Assigning..." : "Assign"}
              </button>
            </form>

            <button
              className="admin-users-page__btn admin-users-page__btn--secondary"
              onClick={() => setManagingUserId(null)}
            >
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
