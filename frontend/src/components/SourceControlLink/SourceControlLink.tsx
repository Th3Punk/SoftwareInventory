import "./SourceControlLink.css";

interface SourceControlLinkProps {
  type: string;
  url: string;
}

const ICONS: Record<string, string> = {
  Git: "🔀",
  AzureDevOps: "🔷",
};

export function SourceControlLink({ type, url }: SourceControlLinkProps) {
  const icon = ICONS[type] ?? "📦";

  return (
    <a
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      className="source-control-link"
      title={`${type} repository`}
    >
      <span className="source-control-link__icon">{icon}</span>
      <span className="source-control-link__label">{type}</span>
      <span className="source-control-link__url">{url}</span>
    </a>
  );
}
