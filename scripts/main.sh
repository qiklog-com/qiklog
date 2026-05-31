#!/usr/bin/env bash
# Entry point: ./scripts/main.sh <script-name>
# Example: make azure-setup  →  ./scripts/main.sh azure-setup

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

require "functions.sh"
require_env_file

run_script "${1:-}"
