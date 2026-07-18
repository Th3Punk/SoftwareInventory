import { useState, useCallback } from "react";
import { Link } from "react-router-dom";
import { useSearch } from "../../hooks/useSearch";
import type { SearchFilters } from "../../api/types";
import "./SearchPage.css";

const RESOURCE_TYPES = ["Application", "Documentation"];

export function SearchPage() {
  const [term, setTerm] = useState("");
  const [submitted, setSubmitted] = useState("");
  const [typeFilter, setTypeFilter] = useState<string[]>([]);
  const [page, setPage] = useState(1);

  const filters: SearchFilters | null = submitted
    ? { q: submitted, type: typeFilter.length > 0 ? typeFilter : undefined, page, pageSize: 20 }
    : null;

  const { data, loading, error } = useSearch(filters);

  const handleSubmit = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      setSubmitted(term.trim());
      setPage(1);
    },
    [term],
  );

  const toggleType = (type: string) => {
    setTypeFilter((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type],
    );
    setPage(1);
  };

  const resultLink = (resourceType: string, resourceId: number) => {
    if (resourceType === "Application") return `/applications/${resourceId}`;
    return `#doc-${resourceId}`;
  };

  return (
    <div className="search-page">
      <h1>Search</h1>

      <form className="search-page__form" onSubmit={handleSubmit}>
        <input
          type="text"
          className="search-page__input"
          value={term}
          onChange={(e) => setTerm(e.target.value)}
          placeholder="Search applications and documentation..."
          autoFocus
        />
        <button type="submit" className="search-page__btn" disabled={!term.trim()}>
          Search
        </button>
      </form>

      <div className="search-page__filters">
        {RESOURCE_TYPES.map((type) => (
          <label key={type} className="search-page__type-label">
            <input
              type="checkbox"
              checked={typeFilter.includes(type)}
              onChange={() => toggleType(type)}
            />
            {type}
          </label>
        ))}
      </div>

      {loading && <p className="search-page__status">Searching...</p>}
      {error && <p className="search-page__error">{error}</p>}

      {data && (
        <div className="search-page__results">
          <p className="search-page__count">
            {data.totalCount} result{data.totalCount !== 1 ? "s" : ""} for &ldquo;{submitted}&rdquo;
          </p>

          {data.items.length === 0 && <p className="search-page__empty">No results found.</p>}

          <ul className="search-page__list">
            {data.items.map((item) => (
              <li key={`${item.resourceType}-${item.resourceId}`} className="search-page__item">
                <span className="search-page__type-badge">{item.resourceType}</span>
                <Link
                  to={resultLink(item.resourceType, item.resourceId)}
                  className="search-page__title"
                >
                  {item.title}
                </Link>
                {item.snippet && (
                  <p
                    className="search-page__snippet"
                    dangerouslySetInnerHTML={{ __html: item.snippet }}
                  />
                )}
              </li>
            ))}
          </ul>

          {data.totalCount > data.pageSize && (
            <div className="search-page__pagination">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="search-page__page-btn"
              >
                Previous
              </button>
              <span>
                Page {page} of {Math.ceil(data.totalCount / data.pageSize)}
              </span>
              <button
                disabled={page * data.pageSize >= data.totalCount}
                onClick={() => setPage((p) => p + 1)}
                className="search-page__page-btn"
              >
                Next
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
