import { useState, useEffect, useRef } from "react";
import { search } from "../api/client";
import type { SearchFilters, SearchResponse } from "../api/types";

export function useSearch(filters: SearchFilters | null) {
  const [data, setData] = useState<SearchResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!filters || !filters.q.trim()) {
      setData(null);
      setError(null);
      return;
    }

    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    setLoading(true);
    setError(null);

    search(filters)
      .then((result) => {
        if (!controller.signal.aborted) {
          setData(result);
        }
      })
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setError(err instanceof Error ? err.message : "Search failed");
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });

    return () => controller.abort();
  }, [filters]);

  return { data, loading, error };
}
