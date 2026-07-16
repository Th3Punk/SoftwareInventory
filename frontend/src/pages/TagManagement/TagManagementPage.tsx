import { useState, useEffect } from "react";
import { fetchTags, createTag, deleteTag } from "../../api/client";
import { TagBadge } from "../../components/TagBadge";
import type { Tag } from "../../api/types";
import "./TagManagementPage.css";

export function TagManagementPage() {
  const [tags, setTags] = useState<Tag[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newName, setNewName] = useState("");
  const [newColor, setNewColor] = useState("#6b7280");
  const [creating, setCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const [deletingId, setDeletingId] = useState<number | null>(null);

  useEffect(() => {
    fetchTags()
      .then(setTags)
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load tags"))
      .finally(() => setLoading(false));
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) return;
    setCreating(true);
    setCreateError(null);
    try {
      const created = await createTag(newName.trim(), newColor);
      setTags((prev) => [...prev, created].sort((a, b) => a.name.localeCompare(b.name)));
      setNewName("");
      setNewColor("#6b7280");
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : "Failed to create tag");
    } finally {
      setCreating(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm("Delete this tag? It will be removed from all applications.")) return;
    setDeletingId(id);
    try {
      await deleteTag(id);
      setTags((prev) => prev.filter((t) => t.id !== id));
    } catch (err: unknown) {
      alert(err instanceof Error ? err.message : "Failed to delete tag");
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="tag-mgmt-page">
      <h1>Tag Management</h1>

      <section className="tag-mgmt-page__create">
        <h2>Create Tag</h2>
        <form className="tag-mgmt-page__form" onSubmit={handleCreate}>
          <input
            type="text"
            className="tag-mgmt-page__input"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Tag name"
            maxLength={100}
            required
          />
          <label className="tag-mgmt-page__color-label">
            Color
            <input
              type="color"
              className="tag-mgmt-page__color"
              value={newColor}
              onChange={(e) => setNewColor(e.target.value)}
            />
          </label>
          <button
            type="submit"
            className="tag-mgmt-page__btn"
            disabled={creating || !newName.trim()}
          >
            {creating ? "Creating..." : "Create"}
          </button>
        </form>
        {createError && <p className="tag-mgmt-page__error">{createError}</p>}
        {newName && (
          <div className="tag-mgmt-page__preview">
            Preview: <TagBadge name={newName} color={newColor} />
          </div>
        )}
      </section>

      <section className="tag-mgmt-page__list-section">
        <h2>All Tags ({tags.length})</h2>

        {loading && <p>Loading...</p>}
        {error && <p className="tag-mgmt-page__error">{error}</p>}

        {!loading && tags.length === 0 && <p className="tag-mgmt-page__empty">No tags yet.</p>}

        <ul className="tag-mgmt-page__list">
          {tags.map((tag) => (
            <li key={tag.id} className="tag-mgmt-page__item">
              <TagBadge name={tag.name} color={tag.color} />
              <button
                className="tag-mgmt-page__delete-btn"
                onClick={() => handleDelete(tag.id)}
                disabled={deletingId === tag.id}
                aria-label={`Delete tag ${tag.name}`}
              >
                {deletingId === tag.id ? "..." : "Delete"}
              </button>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
