import { useContext } from "react";
import { FeatureContext } from "./featureFlags";

export function useFeature(name: string): { enabled: boolean } {
  const flags = useContext(FeatureContext);
  const feature = flags[name];
  return { enabled: feature?.enabled ?? false };
}
