# Frontend - Troubleshooting

## Common Issues

### API requests fail with CORS errors

**Cause:** The Vite dev server proxy is not configured, or the API is not running on port 5000.

**Fix:** Ensure the API is running (`just run`) and that `vite.config.ts` has the proxy configured for `/api`.

### 401 Unauthorized on API calls

**Cause:** The session cookie is not being sent, or the session has expired.

**Fix:** Ensure `credentials: "include"` is set on all fetch requests. The API client does this automatically. Check that the cookie domain matches.

### Feature flags not loading

**Cause:** The `/api/v1/config/features` endpoint is unreachable or returning an error.

**Fix:** The `FeatureProvider` silently falls back to empty flags. Check the browser network tab for the request status.

### Non-public environments not visible

**Cause:** The authenticated user has `ReadOnly` role. Non-public environments are filtered server-side.

**Fix:** This is by design. Only `ApplicationOwner`, `Developer`, and `Admin` roles can see non-public environments.

### TypeScript errors after updating types

**Cause:** The frontend types in `src/api/types.ts` are out of sync with the API response.

**Fix:** Update the TypeScript interfaces in `src/api/types.ts` to match the API response format.

### ESLint react-refresh warnings

**Cause:** A file exports both components and non-component values (hooks, constants).

**Fix:** Move hooks and constants to separate files. Only export React components from `.tsx` files.
