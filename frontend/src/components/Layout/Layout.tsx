import { Link, Outlet } from "react-router-dom";
import "./Layout.css";

export function Layout() {
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
        </nav>
      </header>
      <main className="layout__main">
        <Outlet />
      </main>
    </div>
  );
}
