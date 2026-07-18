import { Link } from "react-router-dom";
import type { ApplicationListItem } from "../../api/types";
import { TagBadge } from "../TagBadge";
import "./ApplicationCard.css";

interface ApplicationCardProps {
  app: ApplicationListItem;
}

const STATUS_COLORS: Record<string, string> = {
  Active: "#16a34a",
  Maintenance: "#ca8a04",
  Deprecated: "#dc2626",
  Retired: "#6b7280",
};

export function ApplicationCard({ app }: ApplicationCardProps) {
  return (
    <Link to={`/applications/${app.id}`} className="app-card">
      <div className="app-card__header">
        <h3 className="app-card__name">{app.name}</h3>
        <span
          className="app-card__status"
          style={{ color: STATUS_COLORS[app.status] ?? "#6b7280" }}
        >
          {app.status}
        </span>
      </div>
      <p className="app-card__description">{app.shortDescription}</p>
      <div className="app-card__meta">
        <span className="app-card__type">{app.type}</span>
        <span className="app-card__team">{app.ownerTeam}</span>
      </div>
      {app.tags.length > 0 && (
        <div className="app-card__tags">
          {app.tags.map((tag) => (
            <TagBadge key={tag} name={tag} />
          ))}
        </div>
      )}
    </Link>
  );
}
