import "./TagBadge.css";

interface TagBadgeProps {
  name: string;
  color?: string | null;
}

export function TagBadge({ name, color }: TagBadgeProps) {
  return (
    <span
      className="tag-badge"
      style={color ? { backgroundColor: color, color: getContrastColor(color) } : undefined}
    >
      {name}
    </span>
  );
}

function getContrastColor(hex: string): string {
  const r = parseInt(hex.slice(1, 3), 16);
  const g = parseInt(hex.slice(3, 5), 16);
  const b = parseInt(hex.slice(5, 7), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? "#000000" : "#ffffff";
}
