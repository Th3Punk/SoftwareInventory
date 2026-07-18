import { createContext } from "react";
import type { FeatureFlags } from "../api/types";

export const FeatureContext = createContext<FeatureFlags>({});
