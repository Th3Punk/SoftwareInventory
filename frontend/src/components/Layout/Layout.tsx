import { Link, Outlet, useNavigate } from "react-router-dom";
import { useFeature } from "../../features/useFeature";
import { useAuth } from "../../context/AuthContext";
import { logout } from "../../api/client";
import "./Layout.css";

export function Layout() {
  const searchEnabled = useFeature("search");
  const catalogEnabled = useFeature("applicationCatalog");
  const adminEnabled = useFeature("admin");
  const { user, setUser } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
    } catch {
      // ignore — clear session regardless
    }
    setUser(null);
    navigate("/login", { replace: true });
  };

  return (
    <div className="layout">
      <header className="layout__header">
        <Link to="/" className="layout__logo">
          SoftwareInventory
        </Link>
        <nav className="layout__nav">
          <Link to="/" className="layout__nav-link">
            Applications
          </Link>
          {searchEnabled.enabled && (
            <Link to="/search" className="layout__nav-link">
              Search
            </Link>
          )}
          {catalogEnabled.enabled && (
            <Link to="/admin/tags" className="layout__nav-link">
              Tags
            </Link>
          )}
          {adminEnabled.enabled && (
            <>
              <Link to="/admin/users" className="layout__nav-link">
                Users
              </Link>
              <Link to="/admin/group-role-mappings" className="layout__nav-link">
                Group Mappings
              </Link>
              <Link to="/admin/audit-logs" className="layout__nav-link">
                Audit Log
              </Link>
            </>
          )}
        </nav>
        <div className="layout__user">
          {user && (
            <>
              <span className="layout__user-name">{user.displayName}</span>
              <button className="layout__logout-btn" onClick={handleLogout}>
                Sign out
              </button>
            </>
          )}
        </div>
      </header>
      <main className="layout__main">
        <Outlet />
      </main>
    </div>
  );
}
