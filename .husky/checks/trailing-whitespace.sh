#!/usr/bin/env bash
#
# Check for trailing whitespace and missing EOF newline in staged files.

set -euo pipefail

staged_files=$(git diff --cached --name-only --diff-filter=ACM | grep -vE '\.(png|jpg|jpeg|gif|ico|woff2?|ttf|eot|svg)$' || true)
[ -z "$staged_files" ] && exit 0

found=0

for file in $staged_files; do
  [ ! -f "$file" ] && continue

  ws=$(grep -nE '\s+$' "$file" 2>/dev/null || true)
  if [ -n "$ws" ]; then
    if [ "$found" -eq 0 ]; then
      echo "⚠ Trailing whitespace found:"
      echo ""
    fi
    echo "  $file:"
    echo "$ws" | head -5 | sed 's/^/    /'
    found=1
  fi

  if [ -s "$file" ] && [ "$(tail -c 1 "$file" | wc -l)" -eq 0 ]; then
    if [ "$found" -eq 0 ]; then
      echo "⚠ Issues found in staged files:"
      echo ""
    fi
    echo "  $file: missing newline at end of file"
    found=1
  fi
done

if [ "$found" -eq 1 ]; then
  echo ""
  echo "Fix these issues before committing."
  exit 1
fi
