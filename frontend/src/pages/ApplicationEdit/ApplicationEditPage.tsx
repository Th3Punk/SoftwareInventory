import { useState, useEffect, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  createApplication,
  updateApplication,
  fetchApplication,
  fetchTags,
} from "../../api/client";
import type { SaveApplicationRequest, Tag } from "../../api/types";
import "./ApplicationEditPage.css";

const STATUS_OPTIONS = ["Active", "Maintenance", "Deprecated", "Retired"];
const TYPE_OPTIONS = ["WebApp", "ApiService", "Library", "BatchJob", "MobileApp", "Other"];
const SOURCE_CONTROL_OPTIONS = ["None", "Git", "AzureDevOps"];

export function ApplicationEditPage() {
  const { id } = useParams<{ id: string }>();
  const appId = id ? Number(id) : null;
  const isEdit = appId !== null;
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [shortDescription, setShortDescription] = useState("");
  const [detailedDescription, setDetailedDescription] = useState("");
  const [status, setStatus] = useState("Active");
  const [type, setType] = useState("WebApp");
  const [ownerTeam, setOwnerTeam] = useState("");
  const [sourceControl, setSourceControl] = useState("None");
  const [repositoryUrl, setRepositoryUrl] = useState("");
  const [wikiUrl, setWikiUrl] = useState("");
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);

  const [allTags, setAllTags] = useState<Tag[]>([]);
  const [appTagNames, setAppTagNames] = useState<string[]>([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchTags()
      .then(setAllTags)
      .catch(() => setAllTags([]));
  }, []);

  useEffect(() => {
    if (!isEdit || appId === null) return;
    fetchApplication(appId)
      .then((app) => {
        setName(app.name);
        setShortDescription(app.shortDescription);
        setDetailedDescription(app.detailedDescription ?? "");
        setStatus(app.status);
        setType(app.type);
        setOwnerTeam(app.ownerTeam);
        setSourceControl(app.sourceControl);
        setRepositoryUrl(app.repositoryUrl ?? "");
        setWikiUrl(app.wikiUrl ?? "");
        setAppTagNames(app.tags);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Failed to load"))
      .finally(() => setLoading(false));
  }, [appId, isEdit]);

  // map the app's tag names to ids once the full tag list is available
  useEffect(() => {
    if (allTags.length === 0 || appTagNames.length === 0) return;
    setSelectedTagIds(allTags.filter((t) => appTagNames.includes(t.name)).map((t) => t.id));
  }, [allTags, appTagNames]);

  const toggleTag = (tagId: number) => {
    setSelectedTagIds((prev) =>
      prev.includes(tagId) ? prev.filter((t) => t !== tagId) : [...prev, tagId],
    );
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    const payload: SaveApplicationRequest = {
      name: name.trim(),
      shortDescription: shortDescription.trim(),
      detailedDescription: detailedDescription.trim() || null,
      status,
      type,
      ownerTeam: ownerTeam.trim(),
      sourceControl,
      repositoryUrl: repositoryUrl.trim() || null,
      wikiUrl: wikiUrl.trim() || null,
      tagIds: selectedTagIds,
    };

    try {
      const saved =
        appId !== null
          ? await updateApplication(appId, payload)
          : await createApplication(payload);
      navigate(`/applications/${saved.id}`);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Save failed");
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <p className="app-edit__status">Loading...</p>;

  return (
    <div className="app-edit">
      <h1>{isEdit ? "Edit Application" : "New Application"}</h1>

      {error && <p className="app-edit__error">{error}</p>}

      <form onSubmit={handleSubmit} className="app-edit__form">
        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="name">
            Name *
          </label>
          <input
            id="name"
            className="app-edit__input"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            maxLength={200}
          />
        </div>

        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="short">
            Short description *
          </label>
          <input
            id="short"
            className="app-edit__input"
            value={shortDescription}
            onChange={(e) => setShortDescription(e.target.value)}
            required
            maxLength={500}
          />
        </div>

        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="detailed">
            Detailed description
          </label>
          <textarea
            id="detailed"
            className="app-edit__textarea"
            value={detailedDescription}
            onChange={(e) => setDetailedDescription(e.target.value)}
            maxLength={4000}
            rows={5}
          />
        </div>

        <div className="app-edit__grid">
          <div className="app-edit__row">
            <label className="app-edit__label" htmlFor="status">
              Status
            </label>
            <select
              id="status"
              className="app-edit__select"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
            >
              {STATUS_OPTIONS.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>

          <div className="app-edit__row">
            <label className="app-edit__label" htmlFor="type">
              Type
            </label>
            <select
              id="type"
              className="app-edit__select"
              value={type}
              onChange={(e) => setType(e.target.value)}
            >
              {TYPE_OPTIONS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="team">
            Owner team *
          </label>
          <input
            id="team"
            className="app-edit__input"
            value={ownerTeam}
            onChange={(e) => setOwnerTeam(e.target.value)}
            required
            maxLength={200}
          />
        </div>

        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="sourceControl">
            Source control
          </label>
          <select
            id="sourceControl"
            className="app-edit__select"
            value={sourceControl}
            onChange={(e) => setSourceControl(e.target.value)}
          >
            {SOURCE_CONTROL_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>

        {sourceControl !== "None" && (
          <div className="app-edit__row">
            <label className="app-edit__label" htmlFor="repo">
              Repository URL *
            </label>
            <input
              id="repo"
              className="app-edit__input"
              value={repositoryUrl}
              onChange={(e) => setRepositoryUrl(e.target.value)}
              placeholder="https://..."
              maxLength={2000}
            />
          </div>
        )}

        <div className="app-edit__row">
          <label className="app-edit__label" htmlFor="wiki">
            Wiki URL
          </label>
          <input
            id="wiki"
            className="app-edit__input"
            value={wikiUrl}
            onChange={(e) => setWikiUrl(e.target.value)}
            placeholder="https://..."
            maxLength={2000}
          />
        </div>

        {allTags.length > 0 && (
          <div className="app-edit__row">
            <label className="app-edit__label">Tags</label>
            <div className="app-edit__tags">
              {allTags.map((tag) => (
                <label key={tag.id} className="app-edit__tag">
                  <input
                    type="checkbox"
                    checked={selectedTagIds.includes(tag.id)}
                    onChange={() => toggleTag(tag.id)}
                  />
                  {tag.name}
                </label>
              ))}
            </div>
          </div>
        )}

        <div className="app-edit__actions">
          <button
            type="button"
            className="app-edit__btn app-edit__btn--cancel"
            onClick={() => navigate(isEdit ? `/applications/${appId}` : "/")}
          >
            Cancel
          </button>
          <button
            type="submit"
            className="app-edit__btn app-edit__btn--save"
            disabled={saving || !name.trim() || !shortDescription.trim() || !ownerTeam.trim()}
          >
            {saving ? "Saving..." : isEdit ? "Save Changes" : "Create"}
          </button>
        </div>
      </form>
    </div>
  );
}
