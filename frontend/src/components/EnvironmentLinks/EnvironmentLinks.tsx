import type { Environment } from "../../api/types";
import "./EnvironmentLinks.css";

interface EnvironmentLinksProps {
  environments: Environment[];
  highlight?: string[];
}

const TYPE_ORDER = ["Production", "Test", "UAT", "Development"];

export function EnvironmentLinks({
  environments,
  highlight = ["Production", "Test"],
}: EnvironmentLinksProps) {
  const sorted = [...environments].sort(
    (a, b) => (TYPE_ORDER.indexOf(a.type) ?? 99) - (TYPE_ORDER.indexOf(b.type) ?? 99),
  );

  return (
    <div className="environment-links">
      {sorted.map((env) => (
        <a
          key={env.id}
          href={env.url}
          target="_blank"
          rel="noopener noreferrer"
          className={`environment-links__item ${highlight.includes(env.type) ? "environment-links__item--highlighted" : ""}`}
          title={env.notes ?? undefined}
        >
          <span className="environment-links__type">{env.type}</span>
          {!env.isPublic && <span className="environment-links__private">internal</span>}
        </a>
      ))}
      {environments.length === 0 && (
        <span className="environment-links__empty">No environments configured</span>
      )}
    </div>
  );
}
