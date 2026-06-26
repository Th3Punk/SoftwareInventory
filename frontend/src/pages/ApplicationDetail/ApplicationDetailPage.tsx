import { Link, useParams } from "react-router-dom";
import { useApplication } from "../../hooks/useApplication";
import { TagBadge } from "../../components/TagBadge";
import { SourceControlLink } from "../../components/SourceControlLink";
import { EnvironmentLinks } from "../../components/EnvironmentLinks";
import "./ApplicationDetailPage.css";

const STATUS_COLORS: Record<string, string> = {
  Active: "#16a34a",
  Maintenance: "#ca8a04",
  Deprecated: "#dc2626",
  Retired: "#6b7280",
};

export function ApplicationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const appId = Number(id);
  const { data: app, loading, error } = useApplication(appId);

  if (loading) return <p>Loading...</p>;
  if (error) return <p className="app-detail__error">{error}</p>;
  if (!app) return <p>Application not found.</p>;

  return (
    <div className="app-detail">
      <Link to="/" className="app-detail__back">
        &larr; Back to list
      </Link>

      <div className="app-detail__header">
        <h1 className="app-detail__name">{app.name}</h1>
        <span
          className="app-detail__status"
          style={{ color: STATUS_COLORS[app.status] ?? "#6b7280" }}
        >
          {app.status}
        </span>
      </div>

      <div className="app-detail__meta">
        <span>Type: {app.type}</span>
        <span>Team: {app.ownerTeam}</span>
        {app.createdByName && <span>Created by: {app.createdByName}</span>}
        <span>Created: {new Date(app.createdAt).toLocaleDateString()}</span>
        <span>Updated: {new Date(app.updatedAt).toLocaleDateString()}</span>
      </div>

      <p className="app-detail__description">{app.shortDescription}</p>
      {app.detailedDescription && (
        <div className="app-detail__detailed">{app.detailedDescription}</div>
      )}

      {app.tags.length > 0 && (
        <div className="app-detail__tags">
          {app.tags.map((tag) => (
            <TagBadge key={tag} name={tag} />
          ))}
        </div>
      )}

      {app.repositoryUrl && (
        <section className="app-detail__section">
          <h2>Source Code</h2>
          <SourceControlLink type={app.sourceControl} url={app.repositoryUrl} />
          {app.wikiUrl && (
            <a
              href={app.wikiUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="app-detail__wiki-link"
            >
              Wiki
            </a>
          )}
        </section>
      )}

      {(app.type === "WebApp" || app.type === "ApiService") && (
        <section className="app-detail__section">
          <h2>Environments</h2>
          <EnvironmentLinks environments={app.environments} highlight={["Production", "Test"]} />
        </section>
      )}

      {app.environments.length > 0 && app.type !== "WebApp" && app.type !== "ApiService" && (
        <section className="app-detail__section">
          <h2>Environments</h2>
          <EnvironmentLinks environments={app.environments} />
        </section>
      )}

      {app.contacts.length > 0 && (
        <section className="app-detail__section">
          <h2>Contacts</h2>
          <div className="app-detail__contacts">
            {app.contacts.map((contact) => (
              <div key={contact.id} className="app-detail__contact">
                <span className="app-detail__contact-type">{contact.type}</span>
                <span className="app-detail__contact-value">{contact.value}</span>
                {contact.label && (
                  <span className="app-detail__contact-label">{contact.label}</span>
                )}
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
