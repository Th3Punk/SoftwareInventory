import { Link, Outlet } from "react-router-dom";
import { useFeature } from "../../features/useFeature";
import "./Layout.css";

export function Layout() {
  const searchEnabled = useFeature("search");
  const catalogEnabled = useFeature("applicationCatalog");

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
        </nav>
      </header>
      <main className="layout__main">
        <Outlet />
      </main>
    </div>
  );
}
