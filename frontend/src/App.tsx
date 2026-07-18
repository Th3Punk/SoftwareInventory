import { useEffect, type ReactNode } from "react";
import { BrowserRouter, Navigate, Routes, Route, useNavigate, useLocation } from "react-router-dom";
import { FeatureProvider } from "./features/FeatureContext";
import { AuthProvider, useAuth } from "./context/AuthContext";
import { Layout } from "./components/Layout";
import { ApplicationListPage } from "./pages/ApplicationList";
import { ApplicationDetailPage } from "./pages/ApplicationDetail";
import { ApplicationEditPage } from "./pages/ApplicationEdit";
import { SearchPage } from "./pages/Search";
import { TagManagementPage } from "./pages/TagManagement";
import { DocumentationEditPage } from "./pages/DocumentationEdit";
import { AdminAuditLogPage } from "./pages/AdminAuditLog";
import { AdminUsersPage } from "./pages/AdminUsers";
import { AdminGroupRoleMappingsPage } from "./pages/AdminGroupRoleMappings";
import { LoginPage } from "./pages/Login";
import { ChangePasswordPage } from "./pages/ChangePassword";

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (loading) return;
    if (!user) {
      navigate("/login", { replace: true });
    } else if (user.mustChangePassword && location.pathname !== "/change-password") {
      navigate("/change-password", { replace: true });
    }
  }, [user, loading, navigate, location.pathname]);

  if (loading) return <div style={{ padding: "2rem" }}>Loading…</div>;
  if (!user) return null;
  if (user.mustChangePassword && location.pathname !== "/change-password") return null;
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
              path="/change-password"
              element={
                <RequireAuth>
                  <ChangePasswordPage />
                </RequireAuth>
              }
            />
            <Route
              element={
                <RequireAuth>
                  <Layout />
                </RequireAuth>
              }
            >
              <Route index element={<ApplicationListPage />} />
              <Route path="applications/new" element={<ApplicationEditPage />} />
              <Route path="applications/:id" element={<ApplicationDetailPage />} />
              <Route path="applications/:id/edit" element={<ApplicationEditPage />} />
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
