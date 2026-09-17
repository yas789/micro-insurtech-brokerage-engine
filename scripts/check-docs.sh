#!/usr/bin/env sh
set -eu

required_files="
README.md
AGENTS.md
docs/architecture.md
docs/specification.md
docs/setup.md
docs/implementation-roadmap.md
docs/branching-strategy.md
docs/ci-cd.md
docs/core-orchestration-natural-language.md
"

for file in $required_files; do
  if [ ! -s "$file" ]; then
    printf 'Missing or empty required documentation file: %s\n' "$file" >&2
    exit 1
  fi
done

printf 'Documentation check passed.\n'
