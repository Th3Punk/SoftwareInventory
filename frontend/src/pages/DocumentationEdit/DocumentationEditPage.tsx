import { useState, useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import MDEditor from "@uiw/react-md-editor";
import { fetchDocumentation, createDocumentation, updateDocumentation } from "../../api/client";
import "./DocumentationEditPage.css";

const DOC_TYPES = ["User", "Developer", "Operations"];
const MAX_BYTES = 512_000;

function byteCount(str: string): number {
  return new TextEncoder().encode(str).length;
}

export function DocumentationEditPage() {
  const { id: appIdStr, docId: docIdStr } = useParams<{ id: string; docId?: string }>();
  const appId = Number(appIdStr);
  const docId = docIdStr ? Number(docIdStr) : null;
  const isEdit = docId !== null;
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [type, setType] = useState("User");
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const contentBytes = byteCount(content);
  const overLimit = contentBytes > MAX_BYTES;

  useEffect(() => {
    if (!isEdit || docId === null) return;
    fetchDocumentation(appId, docId)
      .then((doc) => {
        setTitle(doc.title);
        setContent(doc.content);
        setType(doc.type);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [appId, docId, isEdit]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (overLimit) return;
    setSaving(true);
    setError(null);
    try {
      if (isEdit && docId !== null) {
        await updateDocumentation(appId, docId, { title, content, type });
      } else {
        await createDocumentation(appId, { title, content, type });
      }
      navigate(`/applications/${appId}`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <p className="doc-edit__status">Loading...</p>;

  return (
    <div className="doc-edit" data-color-mode="light">
      <h1>{isEdit ? "Edit Documentation" : "New Documentation"}</h1>

      {error && <p className="doc-edit__error">{error}</p>}

      <form onSubmit={handleSubmit} className="doc-edit__form">
        <div className="doc-edit__row">
          <label className="doc-edit__label" htmlFor="doc-title">
            Title
          </label>
          <input
            id="doc-title"
            className="doc-edit__input"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            maxLength={500}
          />
        </div>

        <div className="doc-edit__row">
          <label className="doc-edit__label" htmlFor="doc-type">
            Type
          </label>
          <select
            id="doc-type"
            className="doc-edit__select"
            value={type}
            onChange={(e) => setType(e.target.value)}
          >
            {DOC_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </div>

        <div className="doc-edit__row doc-edit__row--editor">
          <label className="doc-edit__label">Content</label>
          <div className="doc-edit__editor-wrap">
            <MDEditor
              value={content}
              onChange={(val) => setContent(val ?? "")}
              height={480}
              preview="live"
            />
            <p className={`doc-edit__byte-count ${overLimit ? "doc-edit__byte-count--over" : ""}`}>
              {contentBytes.toLocaleString()} / {MAX_BYTES.toLocaleString()} bytes
              {overLimit && " — content exceeds 500 KB limit"}
            </p>
          </div>
        </div>

        <div className="doc-edit__actions">
          <button
            type="button"
            className="doc-edit__btn doc-edit__btn--cancel"
            onClick={() => navigate(`/applications/${appId}`)}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="doc-edit__btn doc-edit__btn--save"
            disabled={saving || overLimit || !title.trim()}
          >
            {saving ? "Saving..." : isEdit ? "Save Changes" : "Create"}
          </button>
        </div>
      </form>
    </div>
  );
}
