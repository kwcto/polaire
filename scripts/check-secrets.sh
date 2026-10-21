#!/usr/bin/env bash
set -euo pipefail

usage() {
    printf '%s\n' 'Usage: scripts/check-secrets.sh staged | history [revision ...]' >&2
    exit 2
}

[[ $# -ge 1 ]] || usage
mode=$1
shift
case "$mode" in
    staged) [[ $# -eq 0 ]] || usage ;;
    history) ;;
    *) usage ;;
esac

version=8.30.1
repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"
git_common=$(git rev-parse --git-common-dir)
git_common=$(cd "$git_common" && pwd -P)
scanner="$git_common/tools/gitleaks/$version/gitleaks"
if [[ ! -x "$scanner" ]]; then
    printf '%s\n' 'Secret scan unavailable. Run scripts/install-gitleaks.sh, then retry.' >&2
    exit 1
fi
if [[ "$("$scanner" version)" != "$version" ]]; then
    printf '%s\n' 'Unexpected Gitleaks version. Run scripts/install-gitleaks.sh, then retry.' >&2
    exit 1
fi

# Use the pinned scanner's complete built-in rules. Local config, inline allow
# comments, and ignore files must not silently weaken these checks.
unset GITLEAKS_CONFIG
export GITLEAKS_CONFIG_TOML='[extend]
useDefault = true'
scan_args=(git --redact=100 --no-banner --no-color --ignore-gitleaks-allow
    --gitleaks-ignore-path /dev/null)

if [[ "$mode" == staged ]]; then
    # These flags inspect git diff --staged, including a partial staging index.
    exec "$scanner" "${scan_args[@]}" --pre-commit --staged "$repo_root"
fi

if [[ "$(git rev-parse --is-shallow-repository)" != false ]]; then
    printf '%s\n' 'Full-history secret scanning requires a complete (non-shallow) clone.' >&2
    exit 1
fi
[[ $# -gt 0 ]] || set -- HEAD
# Only resolved commit IDs reach log-opts; references cannot inject Git options.
# Explicit roots exclude unrelated local refs, including private Codex snapshots.
log_opts='--full-history --root -m --no-ext-diff --no-textconv --no-renames'
for revision in "$@"; do
    commit=$(git rev-parse --verify --end-of-options "$revision^{commit}") || {
        printf '%s\n' 'Cannot resolve an outgoing revision to a commit; secret scan stopped.' >&2
        exit 1
    }
    log_opts="$log_opts $commit"
done
exec "$scanner" "${scan_args[@]}" --log-opts="$log_opts" "$repo_root"
