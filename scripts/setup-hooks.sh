#!/usr/bin/env bash
set -euo pipefail

repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"
git_common=$(git rev-parse --git-common-dir)
git_common=$(cd "$git_common" && pwd -P)

already_active=false
if configured_hooks=$(git config --get core.hooksPath); then
    if [[ "$configured_hooks" != .githooks ]]; then
        printf '%s\n' 'Existing core.hooksPath found. Integrate the secret checks with those hooks first; no configuration was changed.' >&2
        exit 1
    fi
    already_active=true
else
    config_status=$?
    [[ $config_status -eq 1 ]] || exit "$config_status"
fi

if [[ "$already_active" == false ]]; then
    for hook in "$git_common/hooks/"*; do
        [[ -f "$hook" && -x "$hook" ]] || continue
        case "$hook" in *.sample) continue ;; esac
        printf '%s\n' 'Active hooks already exist in the Git hooks directory. Integrate them first; no configuration was changed.' >&2
        exit 1
    done
fi

for hook in .githooks/pre-commit .githooks/pre-push scripts/check-secrets.sh; do
    [[ -x "$hook" ]] || { printf 'Required executable is missing: %s\n' "$hook" >&2; exit 1; }
done
scanner="$git_common/tools/gitleaks/8.30.1/gitleaks"
if [[ ! -x "$scanner" ]] || [[ "$("$scanner" version)" != 8.30.1 ]]; then
    printf '%s\n' 'Run scripts/install-gitleaks.sh before enabling secret checks.' >&2
    exit 1
fi

if [[ "$already_active" == true ]]; then
    printf '%s\n' 'Repository secret-check hooks are already enabled.'
else
    git config --local core.hooksPath .githooks
    printf '%s\n' 'Enabled repository secret checks before commits and pushes.'
fi
