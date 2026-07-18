import { useEffect, useState } from "react";
import type { ApplicationDetail } from "../api/types";
import { fetchApplication } from "../api/client";

export function useApplication(id: number) {
  const [data, setData] = useState<ApplicationDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    fetchApplication(id)
      .then(setData)
      .catch((e) => setError(e instanceof Error ? e.message : "Failed to load application"))
      .finally(() => setLoading(false));
  }, [id]);

  return { data, loading, error };
}
