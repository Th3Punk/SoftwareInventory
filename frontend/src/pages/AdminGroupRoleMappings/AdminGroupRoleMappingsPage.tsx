import { useState, useEffect, useCallback } from "react";
import {
  fetchGroupRoleMappings,
  createGroupRoleMapping,
  updateGroupRoleMapping,
  deleteGroupRoleMapping,
  fetchRoles,
} from "../../api/client";
import type { GroupRoleMapping, Role } from "../../api/types";
import "./AdminGroupRoleMappingsPage.css";

const PROVIDER_TYPES = ["Local", "Ldap", "Oidc"] as const;

export function AdminGroupRoleMappingsPage() {
  const [mappings, setMappings] = useState<GroupRoleMapping[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [providerType, setProviderType] = useState<string>("Local");
  const [externalGroupRef, setExternalGroupRef] = useState("");
  const [roleId, setRoleId] = useState("");
  const [description, setDescription] = useState("");
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    Promise.all([fetchGroupRoleMappings(), fetchRoles()])
      .then(([m, r]) => {
        setMappings(m);
        setRoles(r);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!roleId) return;
    setCreating(true);
    setCreateError(null);
    try {
      await createGroupRoleMapping({
        providerType,
        externalGroupRef: externalGroupRef.trim(),
        roleId: Number(roleId),
        description: description.trim() || undefined,
      });
      setExternalGroupRef("");
      setDescription("");
      setRoleId("");
      load();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : "Failed to create mapping");
    } finally {
      setCreating(false);
    }
  };

  const handleToggleActive = async (m: GroupRoleMapping) => {
    try {
      await updateGroupRoleMapping(m.id, {
        isActive: !m.isActive,
        description: m.description ?? undefined,
      });
      load();
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to update");
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Delete this group-role mapping? This cannot be undone.")) return;
    try {
      await deleteGroupRoleMapping(id);
      setMappings((prev) => prev.filter((m) => m.id !== id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to delete");
    }
  };

  return (
    <div className="group-mappings-page">
      <h1>Group-Role Mappings</h1>

      <section className="group-mappings-page__create">
        <h2>Add Mapping</h2>
        <form className="group-mappings-page__form" onSubmit={handleCreate}>
          <label className="group-mappings-page__field">
            Provider Type
            <select
              className="group-mappings-page__input"
              value={providerType}
              onChange={(e) => setProviderType(e.target.value)}
            >
              {PROVIDER_TYPES.map((pt) => (
                <option key={pt} value={pt}>
                  {pt}
                </option>
              ))}
            </select>
          </label>

          <label className="group-mappings-page__field">
            External Group Ref
            <input
              className="group-mappings-page__input"
              type="text"
              placeholder="e.g. CN=dev-team,OU=groups,DC=corp"
              value={externalGroupRef}
              onChange={(e) => setExternalGroupRef(e.target.value)}
              required
            />
          </label>

          <label className="group-mappings-page__field">
            Role
            <select
              className="group-mappings-page__input"
              value={roleId}
              onChange={(e) => setRoleId(e.target.value)}
              required
            >
              <option value="">Select role...</option>
              {roles.map((r) => (
                <option key={r.id} value={String(r.id)}>
                  {r.name}
                </option>
              ))}
            </select>
          </label>

          <label className="group-mappings-page__field">
            Description
            <input
              className="group-mappings-page__input"
              type="text"
              placeholder="Optional description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              maxLength={500}
            />
          </label>

          <button
            type="submit"
            className="group-mappings-page__btn"
            disabled={creating || !externalGroupRef.trim() || !roleId}
          >
            {creating ? "Adding..." : "Add Mapping"}
          </button>
        </form>
        {createError && <p className="group-mappings-page__error">{createError}</p>}
      </section>

      <section className="group-mappings-page__list">
        <h2>Current Mappings</h2>

        {loading && <p className="group-mappings-page__status">Loading...</p>}
        {error && <p className="group-mappings-page__error">{error}</p>}

        {!loading && mappings.length === 0 && (
          <p className="group-mappings-page__empty">No mappings configured.</p>
        )}

        {!loading && mappings.length > 0 && (
          <div className="group-mappings-page__table-wrapper">
            <table className="group-mappings-page__table">
              <thead>
                <tr>
                  <th>Provider</th>
                  <th>External Group</th>
                  <th>Role</th>
                  <th>Description</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {mappings.map((m) => (
                  <tr key={m.id} className="group-mappings-page__row">
                    <td>
                      <span className="group-mappings-page__provider-badge">{m.providerType}</span>
                    </td>
                    <td className="group-mappings-page__group-ref" title={m.externalGroupRef}>
                      {m.externalGroupRef}
                    </td>
                    <td>{m.roleName}</td>
                    <td>{m.description ?? "—"}</td>
                    <td>
                      <span
                        className={`group-mappings-page__active-badge ${m.isActive ? "group-mappings-page__active-badge--on" : "group-mappings-page__active-badge--off"}`}
                      >
                        {m.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td>
                      <div className="group-mappings-page__actions">
                        <button
                          className={`group-mappings-page__btn group-mappings-page__btn--sm ${m.isActive ? "group-mappings-page__btn--warn" : ""}`}
                          onClick={() => handleToggleActive(m)}
                        >
                          {m.isActive ? "Disable" : "Enable"}
                        </button>
                        <button
                          className="group-mappings-page__btn group-mappings-page__btn--sm group-mappings-page__btn--danger"
                          onClick={() => handleDelete(m.id)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
