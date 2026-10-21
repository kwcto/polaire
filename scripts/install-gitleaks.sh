#!/usr/bin/env bash
set -euo pipefail
umask 077

# Downloads are explicit: hooks never install tools or access the network.
version=8.30.1
repo_root=$(git rev-parse --show-toplevel)
cd "$repo_root"
git_common=$(git rev-parse --git-common-dir)
git_common=$(cd "$git_common" && pwd -P)

case "$(uname -s)" in
    Darwin) platform=darwin ;;
    Linux) platform=linux ;;
    *) printf '%s\n' 'Gitleaks installation supports macOS and Linux only.' >&2; exit 1 ;;
esac
case "$(uname -m)" in
    arm64|aarch64) architecture=arm64 ;;
    x86_64|amd64) architecture=x64 ;;
    *) printf '%s\n' 'Gitleaks installation supports ARM64 and x86-64 only.' >&2; exit 1 ;;
esac

# SHA256 pins checked against both the release asset API digests and:
# https://github.com/gitleaks/gitleaks/releases/download/v8.30.1/gitleaks_8.30.1_checksums.txt
case "$platform/$architecture" in
    darwin/arm64) expected_sha=b40ab0ae55c505963e365f271a8d3846efbc170aa17f2607f13df610a9aeb6a5 ;;
    darwin/x64) expected_sha=dfe101a4db2255fc85120ac7f3d25e4342c3c20cf749f2c20a18081af1952709 ;;
    linux/arm64) expected_sha=e4a487ee7ccd7d3a7f7ec08657610aa3606637dab924210b3aee62570fb4b080 ;;
    linux/x64) expected_sha=551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb ;;
esac

command -v curl >/dev/null || { printf '%s\n' 'curl is required to install Gitleaks.' >&2; exit 1; }
if command -v sha256sum >/dev/null; then
    hash_command=(sha256sum)
elif command -v shasum >/dev/null; then
    hash_command=(shasum -a 256)
else
    printf '%s\n' 'A SHA256 tool (sha256sum or shasum) is required.' >&2
    exit 1
fi

install_dir="$git_common/tools/gitleaks/$version"
mkdir -p "$install_dir"
install_tmp=$(mktemp -d "$install_dir/.install.XXXXXXXX")
trap 'rm -rf "$install_tmp"' EXIT

archive="gitleaks_${version}_${platform}_${architecture}.tar.gz"
url="https://github.com/gitleaks/gitleaks/releases/download/v$version/$archive"
curl --fail --silent --show-error --location --proto '=https' --proto-redir '=https' \
    --tlsv1.2 --connect-timeout 20 --max-time 180 --retry 2 \
    --output "$install_tmp/$archive" "$url"

actual_sha=$("${hash_command[@]}" "$install_tmp/$archive" | awk '{print $1}')
if [[ "$actual_sha" != "$expected_sha" ]]; then
    printf '%s\n' 'Gitleaks checksum mismatch; nothing was installed.' >&2
    exit 1
fi

# Extract only the executable after authenticating the archive against the pin.
tar -xzf "$install_tmp/$archive" -C "$install_tmp" gitleaks
chmod 700 "$install_tmp/gitleaks"
if [[ "$("$install_tmp/gitleaks" version)" != "$version" ]]; then
    printf '%s\n' 'Gitleaks version mismatch; nothing was installed.' >&2
    exit 1
fi
mv -f "$install_tmp/gitleaks" "$install_dir/gitleaks"
printf 'Installed Gitleaks %s in the repository Git tools directory.\n' "$version"
