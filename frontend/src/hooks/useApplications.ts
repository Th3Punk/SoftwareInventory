import { useCallback, useEffect, useState } from "react";
import type { ApplicationFilters, ApplicationListItem, PagedResponse } from "../api/types";
import { fetchApplications } from "../api/client";

export function useApplications(initialFilters: ApplicationFilters = {}) {
  const [data, setData] = useState<PagedResponse<ApplicationListItem> | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filters, setFilters] = useState<ApplicationFilters>(initialFilters);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await fetchApplications(filters);
      setData(result);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load applications");
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, loading, error, filters, setFilters, reload: load };
}
