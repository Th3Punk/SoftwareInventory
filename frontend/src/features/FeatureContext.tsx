import { useEffect, useState, type ReactNode } from "react";
import type { FeatureFlags } from "../api/types";
import { fetchFeatureFlags } from "../api/client";
import { FeatureContext } from "./featureFlags";

export function FeatureProvider({ children }: { children: ReactNode }) {
  const [flags, setFlags] = useState<FeatureFlags>({});

  useEffect(() => {
    fetchFeatureFlags()
      .then(setFlags)
      .catch(() => setFlags({}));
  }, []);

  return <FeatureContext.Provider value={flags}>{children}</FeatureContext.Provider>;
}
