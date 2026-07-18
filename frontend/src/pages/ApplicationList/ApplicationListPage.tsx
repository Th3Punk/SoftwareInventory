import { useApplications } from "../../hooks/useApplications";
import { ApplicationCard } from "../../components/ApplicationCard";
import { SearchBar } from "../../components/SearchBar";
import "./ApplicationListPage.css";

const STATUS_OPTIONS = ["Active", "Maintenance", "Deprecated", "Retired"];
const TYPE_OPTIONS = ["WebApp", "ApiService", "Library", "BatchJob", "MobileApp", "Other"];
const SORT_OPTIONS = [
  { value: "name", label: "Name (A-Z)" },
  { value: "-name", label: "Name (Z-A)" },
  { value: "-updatedAt", label: "Recently Updated" },
  { value: "-createdAt", label: "Newest First" },
  { value: "createdAt", label: "Oldest First" },
];

export function ApplicationListPage() {
  const { data, loading, error, filters, setFilters } = useApplications({
    page: 1,
    pageSize: 20,
    sort: "name",
  });

  const handleSearch = (q: string) => {
    setFilters((prev) => ({ ...prev, q: q || undefined, page: 1 }));
  };

  const handleFilterChange = (key: string, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value || undefined, page: 1 }));
  };

  const handlePageChange = (page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  };

  return (
    <div className="app-list-page">
      <h1>Applications</h1>

      <SearchBar value={filters.q ?? ""} onChange={handleSearch} />

      <div className="app-list-page__filters">
        <select
          value={filters.status ?? ""}
          onChange={(e) => handleFilterChange("status", e.target.value)}
          className="app-list-page__select"
        >
          <option value="">All statuses</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>

        <select
          value={filters.type ?? ""}
          onChange={(e) => handleFilterChange("type", e.target.value)}
          className="app-list-page__select"
        >
          <option value="">All types</option>
          {TYPE_OPTIONS.map((t) => (
            <option key={t} value={t}>
              {t}
            </option>
          ))}
        </select>

        <select
          value={filters.sort ?? "name"}
          onChange={(e) => handleFilterChange("sort", e.target.value)}
          className="app-list-page__select"
        >
          {SORT_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      </div>

      {loading && <p className="app-list-page__status">Loading...</p>}
      {error && <p className="app-list-page__error">{error}</p>}

      {data && (
        <>
          <p className="app-list-page__count">{data.totalCount} application(s) found</p>
          <div className="app-list-page__grid">
            {data.items.map((app) => (
              <ApplicationCard key={app.id} app={app} />
            ))}
          </div>

          {data.totalCount > data.pageSize && (
            <div className="app-list-page__pagination">
              <button
                disabled={data.page <= 1}
                onClick={() => handlePageChange(data.page - 1)}
                className="app-list-page__page-btn"
              >
                Previous
              </button>
              <span className="app-list-page__page-info">
                Page {data.page} of {Math.ceil(data.totalCount / data.pageSize)}
              </span>
              <button
                disabled={data.page * data.pageSize >= data.totalCount}
                onClick={() => handlePageChange(data.page + 1)}
                className="app-list-page__page-btn"
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
