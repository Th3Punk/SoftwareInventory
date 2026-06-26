# Frontend - Configuration

## Development

```bash
cd frontend
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` by default and proxies API requests to `http://localhost:5000`.

## Build

```bash
npm run build
```

Output goes to `frontend/dist/`.

## Linting

```bash
npm run lint        # Check ESLint + Prettier
npm run lint:fix    # Auto-fix ESLint + Prettier
```

## Justfile Commands

```bash
just fe-dev    # Start frontend dev server
just fe-build  # Production build
just lint-frontend  # Run ESLint + Prettier check
```

## Vite Proxy Configuration

In `vite.config.ts`:

```typescript
server: {
  proxy: {
    "/api": {
      target: "http://localhost:5000",
      changeOrigin: true,
    },
  },
}
```

## TypeScript Configuration

- Target: ES2020
- Module: ESNext (bundler resolution)
- Strict mode enabled
- No unused locals/parameters
- No unchecked indexed access

## ESLint Configuration

- Base: `@eslint/js` recommended + `typescript-eslint` recommended
- React Hooks plugin (enforces rules of hooks)
- React Refresh plugin (warns about non-component exports)

## Prettier Configuration

- Semicolons: yes
- Single quotes: no (double quotes)
- Trailing commas: all
- Print width: 100
- Tab width: 2
