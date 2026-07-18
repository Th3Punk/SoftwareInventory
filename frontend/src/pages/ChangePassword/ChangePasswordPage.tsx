import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError, changePassword, fetchCurrentUser } from "../../api/client";
import { useAuth } from "../../context/AuthContext";
import "./ChangePasswordPage.css";

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { setUser } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirm) {
      setError("New passwords do not match.");
      return;
    }
    setError(null);
    setLoading(true);
    try {
      await changePassword(currentPassword, newPassword);
      const updated = await fetchCurrentUser();
      setUser(updated);
      navigate("/", { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 400) {
        setError(err.message);
      } else {
        setError(err instanceof Error ? err.message : "Password change failed");
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="change-password-page">
      <div className="change-password-page__card">
        <h1 className="change-password-page__title">Change Password</h1>
        <p className="change-password-page__hint">
          You must set a new password before continuing.
        </p>
        <form onSubmit={handleSubmit} className="change-password-page__form">
          <div className="change-password-page__field">
            <label htmlFor="current">Current password</label>
            <input
              id="current"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              autoFocus
              autoComplete="current-password"
            />
          </div>
          <div className="change-password-page__field">
            <label htmlFor="new">New password</label>
            <input
              id="new"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              autoComplete="new-password"
            />
          </div>
          <div className="change-password-page__field">
            <label htmlFor="confirm">Confirm new password</label>
            <input
              id="confirm"
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              required
              autoComplete="new-password"
            />
          </div>
          {error && <p className="change-password-page__error">{error}</p>}
          <button type="submit" disabled={loading} className="change-password-page__submit">
            {loading ? "Saving…" : "Set new password"}
          </button>
        </form>
      </div>
    </div>
  );
}
