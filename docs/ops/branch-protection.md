# Branch Protection Configuration

## Protected branches

### `main` (production)

| Setting | Value |
|---------|-------|
| Require pull request before merging | Yes |
| Required approving reviews | 1 |
| Require status checks to pass | Yes |
| Required checks | `Build`, `Test`, `Lint (Backend)`, `Lint (Markdown)`, `Security scan` |
| Require branches to be up to date | Yes |
| Allow force pushes | No |
| Allow deletions | No |

### `test` (staging)

| Setting | Value |
|---------|-------|
| Require pull request before merging | Yes |
| Required approving reviews | 1 |
| Require status checks to pass | Yes |
| Required checks | `Build`, `Test`, `Lint (Backend)`, `Lint (Markdown)`, `Security scan` |
| Require branches to be up to date | Yes |
| Allow force pushes | No |
| Allow deletions | No |

## CI jobs overview

| Job | Trigger | Purpose |
|-----|---------|---------|
| **Build** | PR + push to `main`/`test` | `dotnet build` Release mode |
| **Test** | PR + push to `main`/`test` | `dotnet test` with coverage, PostgreSQL service container |
| **Lint (Backend)** | PR + push to `main`/`test` | `dotnet format --verify-no-changes` |
| **Lint (Frontend)** | PR + push to `main`/`test` | ESLint (skipped if `frontend/` does not exist) |
| **Lint (Markdown)** | PR + push to `main`/`test` | `markdownlint-cli2` on `docs/**/*.md` and `*.md` |
| **Security scan** | PR + push to `main`/`test` | NuGet vulnerable packages + npm audit |

## Setup steps (GitHub UI)

1. Go to **Settings → Branches → Add branch protection rule**
2. Enter branch name pattern: `main`
3. Enable the settings from the table above
4. Add the required status checks by name
5. Repeat for `test`

## Notes

- Feature branches merge into `test` via PR
- `test` merges into `main` at phase end (no squash merge, preserves history)
- Direct pushes to `main` and `test` are not allowed
- The `Lint (Frontend)` job auto-skips when no `frontend/` directory exists
