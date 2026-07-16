import { BrowserRouter, Routes, Route } from "react-router-dom";
import { FeatureProvider } from "./features/FeatureContext";
import { Layout } from "./components/Layout";
import { ApplicationListPage } from "./pages/ApplicationList";
import { ApplicationDetailPage } from "./pages/ApplicationDetail";
import { SearchPage } from "./pages/Search";
import { TagManagementPage } from "./pages/TagManagement";
import { DocumentationEditPage } from "./pages/DocumentationEdit";

export function App() {
  return (
    <FeatureProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<Layout />}>
            <Route index element={<ApplicationListPage />} />
            <Route path="applications/:id" element={<ApplicationDetailPage />} />
            <Route path="applications/:id/docs/new" element={<DocumentationEditPage />} />
            <Route path="applications/:id/docs/:docId/edit" element={<DocumentationEditPage />} />
            <Route path="search" element={<SearchPage />} />
            <Route path="admin/tags" element={<TagManagementPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </FeatureProvider>
  );
}
