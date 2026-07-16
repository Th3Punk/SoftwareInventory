import { useState, useEffect, useCallback } from "react";
import { fetchAuditLogs, fetchAuditLog } from "../../api/client";
import type { AuditLog, AuditLogDetail, AuditLogFilters } from "../../api/types";
import type { PagedResponse } from "../../api/types";
import "./AdminAuditLogPage.css";

function tryFormatJson(s: string | null): string {
  if (!s) return "—";
  try {
    return JSON.stringify(JSON.parse(s), null, 2);
  } catch {
    return s;
  }
}

export function AdminAuditLogPage() {
  const [filters, setFilters] = useState<AuditLogFilters>({ page: 1, pageSize: 20 });
  const [formState, setFormState] = useState({
    resourceType: "",
    resourceId: "",
    action: "",
    userId: "",
    from: "",
    to: "",
  });

  const [data, setData] = useState<PagedResponse<AuditLog> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [detail, setDetail] = useState<AuditLogDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    fetchAuditLogs(filters)
      .then(setData)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  const handleFilter = (e: React.FormEvent) => {
    e.preventDefault();
    setFilters({
      resourceType: formState.resourceType || undefined,
      resourceId: formState.resourceId || undefined,
      action: formState.action || undefined,
      userId: formState.userId ? Number(formState.userId) : undefined,
      from: formState.from || undefined,
      to: formState.to || undefined,
      page: 1,
      pageSize: 20,
    });
  };

  const handleRowClick = async (log: AuditLog) => {
    setDetail(null);
    setDetailLoading(true);
    try {
      setDetail(await fetchAuditLog(log.id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to load detail");
    } finally {
      setDetailLoading(false);
    }
  };

  const totalPages = data ? Math.ceil(data.totalCount / (data.pageSize || 20)) : 1;
  const currentPage = filters.page ?? 1;

  return (
    <div className="audit-log-page">
      <h1>Audit Log</h1>

      <form className="audit-log-page__filters" onSubmit={handleFilter}>
        <input
          className="audit-log-page__input"
          placeholder="Resource type"
          value={formState.resourceType}
          onChange={(e) => setFormState((f) => ({ ...f, resourceType: e.target.value }))}
        />
        <input
          className="audit-log-page__input"
          placeholder="Resource ID"
          value={formState.resourceId}
          onChange={(e) => setFormState((f) => ({ ...f, resourceId: e.target.value }))}
        />
        <input
          className="audit-log-page__input"
          placeholder="Action"
          value={formState.action}
          onChange={(e) => setFormState((f) => ({ ...f, action: e.target.value }))}
        />
        <input
          className="audit-log-page__input"
          type="number"
          placeholder="User ID"
          value={formState.userId}
          onChange={(e) => setFormState((f) => ({ ...f, userId: e.target.value }))}
        />
        <label className="audit-log-page__date-label">
          From
          <input
            className="audit-log-page__input"
            type="datetime-local"
            value={formState.from}
            onChange={(e) => setFormState((f) => ({ ...f, from: e.target.value }))}
          />
        </label>
        <label className="audit-log-page__date-label">
          To
          <input
            className="audit-log-page__input"
            type="datetime-local"
            value={formState.to}
            onChange={(e) => setFormState((f) => ({ ...f, to: e.target.value }))}
          />
        </label>
        <button type="submit" className="audit-log-page__btn">
          Filter
        </button>
      </form>

      {loading && <p className="audit-log-page__status">Loading...</p>}
      {error && <p className="audit-log-page__error">{error}</p>}

      {data && !loading && (
        <>
          <p className="audit-log-page__count">{data.totalCount} entries</p>
          <div className="audit-log-page__table-wrapper">
            <table className="audit-log-page__table">
              <thead>
                <tr>
                  <th>Timestamp</th>
                  <th>Action</th>
                  <th>Resource Type</th>
                  <th>Resource ID</th>
                  <th>User ID</th>
                  <th>IP Address</th>
                </tr>
              </thead>
              <tbody>
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="audit-log-page__empty">
                      No entries found.
                    </td>
                  </tr>
                )}
                {data.items.map((log) => (
                  <tr
                    key={log.id}
                    className="audit-log-page__row"
                    onClick={() => handleRowClick(log)}
                    title="Click for details"
                  >
                    <td>{new Date(log.timestamp).toLocaleString()}</td>
                    <td>
                      <code>{log.action}</code>
                    </td>
                    <td>{log.resourceType}</td>
                    <td>{log.resourceId}</td>
                    <td>{log.userId ?? "—"}</td>
                    <td>{log.ipAddress ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="audit-log-page__pagination">
            <button
              className="audit-log-page__btn"
              disabled={currentPage <= 1}
              onClick={() => setFilters((f) => ({ ...f, page: currentPage - 1 }))}
            >
              Previous
            </button>
            <span>
              Page {currentPage} of {totalPages}
            </span>
            <button
              className="audit-log-page__btn"
              disabled={currentPage >= totalPages}
              onClick={() => setFilters((f) => ({ ...f, page: currentPage + 1 }))}
            >
              Next
            </button>
          </div>
        </>
      )}

      {(detail || detailLoading) && (
        <div
          className="audit-log-page__modal-overlay"
          onClick={() => {
            setDetail(null);
          }}
        >
          <div className="audit-log-page__modal" onClick={(e) => e.stopPropagation()}>
            {detailLoading && <p>Loading...</p>}
            {detail && (
              <>
                <h2>Audit Entry</h2>
                <dl className="audit-log-page__dl">
                  <dt>Timestamp</dt>
                  <dd>{new Date(detail.timestamp).toLocaleString()}</dd>
                  <dt>Action</dt>
                  <dd>
                    <code>{detail.action}</code>
                  </dd>
                  <dt>Resource</dt>
                  <dd>
                    {detail.resourceType} / {detail.resourceId}
                  </dd>
                  <dt>User ID</dt>
                  <dd>{detail.userId ?? "—"}</dd>
                  <dt>IP Address</dt>
                  <dd>{detail.ipAddress ?? "—"}</dd>
                  <dt>User Agent</dt>
                  <dd>{detail.userAgent ?? "—"}</dd>
                  {detail.oldValueJson && (
                    <>
                      <dt>Old Value</dt>
                      <dd>
                        <pre className="audit-log-page__pre">
                          {tryFormatJson(detail.oldValueJson)}
                        </pre>
                      </dd>
                    </>
                  )}
                  {detail.newValueJson && (
                    <>
                      <dt>New Value</dt>
                      <dd>
                        <pre className="audit-log-page__pre">
                          {tryFormatJson(detail.newValueJson)}
                        </pre>
                      </dd>
                    </>
                  )}
                </dl>
                <button className="audit-log-page__btn" onClick={() => setDetail(null)}>
                  Close
                </button>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
