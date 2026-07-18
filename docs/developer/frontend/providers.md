# Frontend - Providers

## Feature Flag Provider

The `FeatureProvider` component wraps the application and loads feature flags from the API on mount:

```tsx
<FeatureProvider>
  <App />
</FeatureProvider>
```

Components can check flags via the `useFeature` hook:

```tsx
const { enabled } = useFeature("Search");
return enabled ? <SearchBar /> : null;
```

## API Client

The API client is a thin wrapper around `fetch` that:

1. Always sends `credentials: "include"` for cookie-based auth
2. Sets `Content-Type: application/json`
3. Throws `ApiError` with status code on non-2xx responses
4. Returns `undefined` for 204 No Content responses

## Authentication

Phase 1 uses cookie-based sessions. The API client's `credentials: "include"` ensures the `HttpOnly` session cookie is sent with every request. No token management is needed on the frontend.

In a future phase (Kerberos/Negotiate), the same `credentials: "include"` pattern works — the browser handles the Negotiate ticket automatically.

## Future Providers

- **Auth context**: `useAuth` hook for session state, login/logout, MustChangePassword handling
- **Markdown rendering**: `react-markdown` + `rehype-highlight` + `remark-gfm` for documentation viewer
- **Markdown editor**: `@uiw/react-md-editor` for documentation editing
