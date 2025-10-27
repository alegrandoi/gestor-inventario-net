#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
INSTALL_DIR="${INSTALL_DIR:-$REPO_ROOT/.dotnet}"

GLOBAL_JSON="$REPO_ROOT/global.json"

mkdir -p "$INSTALL_DIR"
INSTALL_SCRIPT="$INSTALL_DIR/dotnet-install.sh"
if [[ ! -x "$INSTALL_SCRIPT" ]]; then
    if command -v curl >/dev/null 2>&1; then
        curl -sSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"
    elif command -v wget >/dev/null 2>&1; then
        wget -qO "$INSTALL_SCRIPT" https://dot.net/v1/dotnet-install.sh
    else
        echo "Neither curl nor wget is available to download dotnet-install.sh" >&2
        exit 1
    fi
    chmod +x "$INSTALL_SCRIPT"
fi

INSTALL_ARGS=(--install-dir "$INSTALL_DIR" --no-path)

if [[ -n "${VERSION:-}" ]]; then
    INSTALL_ARGS=(--version "$VERSION" "${INSTALL_ARGS[@]}")
elif [[ -f "$GLOBAL_JSON" ]]; then
    INSTALL_ARGS=(--jsonfile "$GLOBAL_JSON" "${INSTALL_ARGS[@]}")
else
    INSTALL_ARGS=(--version "8.0.100" "${INSTALL_ARGS[@]}")
fi

"$INSTALL_SCRIPT" "${INSTALL_ARGS[@]}"