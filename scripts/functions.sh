# Shared shell helpers for QikLog scripts.
# Sourced by main.sh — do not execute directly.

# ── Color (NO_COLOR=1 disables) ───────────────────────────────────────────────
if [[ -z "${NO_COLOR:-}" ]]; then
  _C_BOLD='\033[1m'
  _C_DIM='\033[2m'
  _C_CYAN='\033[36m'
  _C_GREEN='\033[32m'
  _C_YELLOW='\033[33m'
  _C_RED='\033[31m'
  _C_BLUE='\033[34m'
  _C_RESET='\033[0m'
else
  _C_BOLD='' _C_DIM='' _C_CYAN='' _C_GREEN='' _C_YELLOW='' _C_RED='' _C_BLUE='' _C_RESET=''
fi

log_banner() { printf '\n%s▸ %s%s\n' "$_C_BOLD$_C_BLUE" "$1" "$_C_RESET"; }
log_step()   { printf '%s→%s %s\n' "$_C_CYAN" "$_C_RESET" "$1"; }
log_ok()     { printf '%s✓%s %s\n' "$_C_GREEN" "$_C_RESET" "$1"; }
log_warn()   { printf '%s!%s %s\n' "$_C_YELLOW" "$_C_RESET" "$1"; }
log_fail()   { printf '%s✗%s %s\n' "$_C_RED" "$_C_RESET" "$1" >&2; }

# Source a file under SCRIPT_DIR; exit if missing.
require() {
  local file="$SCRIPT_DIR/$1"
  if [[ -f "$file" ]]; then
    # shellcheck source=/dev/null
    source "$file"
  else
    log_fail "Required file not found: $file"
    exit 1
  fi
}

# Source repo .env (required for Azure scripts).
require_env_file() {
  local env_file="$SCRIPT_DIR/../.env"
  if [[ ! -f "$env_file" ]]; then
    log_fail "Missing $env_file — copy .env.example and fill in values"
    exit 1
  fi
  # shellcheck source=/dev/null
  set -a
  source "$env_file"
  set +a
}

# Fail if a variable is unset or empty.
require_var() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    log_fail "Required environment variable is not set: $name"
    exit 1
  fi
}

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    log_fail "Required command not found: $cmd"
    exit 1
  fi
}

# Run a named script module (e.g. azure-setup).
run_script() {
  local name="$1"
  if [[ -z "$name" ]]; then
    log_fail "Usage: ./scripts/main.sh <script-name>"
    log_step "Example: ./scripts/main.sh azure-setup"
    exit 1
  fi
  require "${name}.sh"
}
