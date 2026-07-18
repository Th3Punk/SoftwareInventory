# Frontend - Overview

## Technology Stack

- **React 19** with TypeScript
- **Vite 6** for build and dev server
- **React Router 7** for client-side routing
- **ESLint 9** + **Prettier 3** for code quality

## Project Structure

```text
frontend/
├── src/
│   ├── api/              # API client (fetch wrapper, TypeScript types)
│   ├── features/         # Feature flag context + useFeature hook
│   ├── hooks/            # Data-fetching hooks (useApplications, useApplication)
│   ├── components/       # Reusable UI components
│   │   ├── ApplicationCard/
│   │   ├── SourceControlLink/
│   │   ├── EnvironmentLinks/
│   │   ├── TagBadge/
│   │   ├── SearchBar/
│   │   └── Layout/
│   ├── pages/
│   │   ├── ApplicationList/
│   │   └── ApplicationDetail/
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── index.html
├── vite.config.ts
├── tsconfig.json
├── eslint.config.js
└── .prettierrc
```

## Key Components

### API Client (`src/api/client.ts`)

All API requests use `credentials: "include"` to send the session cookie. The client wraps `fetch` with error handling that throws `ApiError` with status codes.

### Feature Flags (`src/features/`)

- `FeatureProvider` loads flags from `/api/v1/config/features` on mount
- `useFeature(name)` returns `{ enabled: boolean }` for conditional rendering

### Data Hooks

- `useApplications(filters)` - Paginated, filterable application list
- `useApplication(id)` - Single application detail

### Spec-Required Components (13.6)

- `SourceControlLink` - Renders Git/AzureDevOps icon + link
- `EnvironmentLinks` - Highlights Production/Test environments, shows "internal" badge for non-public

## Dev Server Proxy

Vite proxies `/api` requests to `http://localhost:5000` (the ASP.NET Core API).
