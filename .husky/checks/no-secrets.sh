#!/usr/bin/env bash
#
# Lightweight secret detection for staged files.
# Flags common secret patterns before they reach the repo.

set -euo pipefail

PATTERNS=(
  'password\s*=\s*"[^"]+'
  'PRIVATE[[:space:]]KEY'
  'api[_-]?key\s*[:=]\s*"[^"]+'
  'secret\s*[:=]\s*"[^"]+'
  'connectionstring\s*[:=]\s*"[^"]+'
  'Bearer\s+[A-Za-z0-9\-._~+/]+=*'
)

staged_files=$(git diff --cached --name-only --diff-filter=ACM)
[ -z "$staged_files" ] && exit 0

filtered_files=""
while IFS= read -r file; do
  case "$file" in
    *.example|*.md|appsettings.json|appsettings.*.json)
      continue ;;
    .husky/checks/*)
      continue ;;
    CLAUDE.md)
      continue ;;
  esac
  filtered_files+="$file"$'\n'
done <<< "$staged_files"

filtered_files=$(echo "$filtered_files" | sed '/^$/d')
[ -z "$filtered_files" ] && exit 0

found=0
for pattern in "${PATTERNS[@]}"; do
  matches=$(echo "$filtered_files" | xargs grep -inE "$pattern" 2>/dev/null || true)
  if [ -n "$matches" ]; then
    if [ "$found" -eq 0 ]; then
      echo "⚠ Possible secrets detected in staged files:"
      echo ""
    fi
    echo "$matches"
    found=1
  fi
done

if [ "$found" -eq 1 ]; then
  echo ""
  echo "If these are intentional (e.g., test fixtures), commit with --no-verify."
  exit 1
fi
