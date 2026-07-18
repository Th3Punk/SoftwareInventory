import { useEffect, type ReactNode } from "react";
import { BrowserRouter, Navigate, Routes, Route, useNavigate } from "react-router-dom";
import { FeatureProvider } from "./features/FeatureContext";
import { AuthProvider, useAuth } from "./context/AuthContext";
import { Layout } from "./components/Layout";
import { ApplicationListPage } from "./pages/ApplicationList";
import { ApplicationDetailPage } from "./pages/ApplicationDetail";
import { SearchPage } from "./pages/Search";
import { TagManagementPage } from "./pages/TagManagement";
import { DocumentationEditPage } from "./pages/DocumentationEdit";
import { AdminAuditLogPage } from "./pages/AdminAuditLog";
import { AdminUsersPage } from "./pages/AdminUsers";
import { AdminGroupRoleMappingsPage } from "./pages/AdminGroupRoleMappings";
import { LoginPage } from "./pages/Login";

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!loading && !user) {
      navigate("/login", { replace: true });
    }
  }, [user, loading, navigate]);

  if (loading) return <div style={{ padding: "2rem" }}>Loading…</div>;
  if (!user) return null;
  return <>{children}</>;
}

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <FeatureProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route
              element={
                <RequireAuth>
                  <Layout />
                </RequireAuth>
              }
            >
              <Route index element={<ApplicationListPage />} />
              <Route path="applications/:id" element={<ApplicationDetailPage />} />
              <Route path="applications/:id/docs/new" element={<DocumentationEditPage />} />
              <Route path="applications/:id/docs/:docId/edit" element={<DocumentationEditPage />} />
              <Route path="search" element={<SearchPage />} />
              <Route path="admin/tags" element={<TagManagementPage />} />
              <Route path="admin/audit-logs" element={<AdminAuditLogPage />} />
              <Route path="admin/users" element={<AdminUsersPage />} />
              <Route path="admin/group-role-mappings" element={<AdminGroupRoleMappingsPage />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </FeatureProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
