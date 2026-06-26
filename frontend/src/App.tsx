import { BrowserRouter, Routes, Route } from "react-router-dom";
import { FeatureProvider } from "./features/FeatureContext";
import { Layout } from "./components/Layout";
import { ApplicationListPage } from "./pages/ApplicationList";
import { ApplicationDetailPage } from "./pages/ApplicationDetail";

export function App() {
  return (
    <FeatureProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<ApplicationListPage />} />
            <Route path="applications/:id" element={<ApplicationDetailPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </FeatureProvider>
  );
}
