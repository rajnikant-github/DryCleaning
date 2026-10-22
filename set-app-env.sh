#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# EDry Cleaners – switch the mobile app between environments (macOS / Linux)
#
#   ./set-app-env.sh local                 App → API on this machine (./run-api.sh)
#   ./set-app-env.sh local --device-ip 192.168.1.20
#                                          Same, for a physical phone on your Wi-Fi
#   ./set-app-env.sh mock                  Demo data, no API needed
#   ./set-app-env.sh staging               App → test server
#   ./set-app-env.sh production            App → the live server
#   ./set-app-env.sh --show                Which environment / server the next build uses
#   ./set-app-env.sh --list                Available environments
#   ./set-app-env.sh --reset               Back to the built-in defaults (no environment)
#   ./set-app-env.sh --help                Show this help        (short forms: -s, -l, -h)
#
# ONE file holds every setting for every environment: the repository's git-ignored secrets.json
# (copy secrets.example.json to create it). Each environment is a block ("local", "mock", "staging",
# "production") and its "App" section is the mobile app's settings – API address, features, pricing,
# the Google Maps key. This script copies that App section into DryCleaning/appsettings.secrets.json
# (git-ignored), which is built into the app – rebuild and redeploy the app after switching.
# Only the App section goes into the app (the Api and Admin sections never do): anything in the app can be extracted.
# "permission denied"? Run once: chmod +x set-app-env.sh   (or use: bash set-app-env.sh)
# ─────────────────────────────────────────────────────────────────────────────
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/DryCleaning"
BASE_FILE="$APP_DIR/appsettings.json"                     # built-in defaults (committed, almost empty)
GENERATED="$APP_DIR/appsettings.secrets.json"             # generated – the App section of the chosen environment
SECRETS_JSON="$SCRIPT_DIR/secrets.json"                    # the single file with every setting (git-ignored)
EXAMPLE_JSON="$SCRIPT_DIR/secrets.example.json"
NSC_FILE="$APP_DIR/Platforms/Android/Resources/xml/network_security_config.xml"
API_PORT=5180

bold=$'\033[1m'; dim=$'\033[2m'; green=$'\033[32m'; yellow=$'\033[33m'; red=$'\033[31m'; reset=$'\033[0m'
ok()   { echo "  ${green}✓${reset} $*"; }
warn() { echo "  ${yellow}!${reset} $*"; }
fail() { echo "  ${red}✗${reset} $*" >&2; exit 1; }

usage() { sed -n '3,22p' "$0" | sed 's/^# \{0,1\}//'; }

command -v python3 >/dev/null 2>&1 || fail "python3 is needed to read secrets.json."

# Value of a "Key": value pair in a JSON file (simple string/bool values; keys are unique in our files).
json_value() {
  [ -f "$2" ] || return 0
  sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\{0,1\}\([^\",}]*\)\"\{0,1\}.*/\1/p" "$2" | head -1 | sed 's/[[:space:]]*$//'
}

# Effective value: the generated App section first, then the built-in defaults (same order as the app).
setting() {
  local v; v="$(json_value "$1" "$GENERATED")"
  [ -n "$v" ] || v="$(json_value "$1" "$BASE_FILE")"
  echo "$v"
}

validate_json() {
  python3 -c 'import json,sys; json.load(open(sys.argv[1]))' "$1" 2>/dev/null || fail "$(basename "$1") is not valid JSON."
}

# secrets.json is the one file for everything: create it from the template when it is missing.
ensure_secrets_json() {
  [ -f "$SECRETS_JSON" ] && return 0
  [ -f "$EXAMPLE_JSON" ] || fail "secrets.json and secrets.example.json are both missing – run this script from the repository."
  cp "$EXAMPLE_JSON" "$SECRETS_JSON"
  chmod 600 "$SECRETS_JSON" 2>/dev/null
  warn "secrets.json did not exist – created it from secrets.example.json. Fill in your keys there (git-ignored)."
}

# Environments = the blocks of secrets.json that have an App section.
env_names() {
  [ -f "$SECRETS_JSON" ] || return 0
  python3 - "$SECRETS_JSON" <<'PY'
import json, sys
try:
    data = json.load(open(sys.argv[1], encoding="utf-8-sig"))
except ValueError as e:
    sys.exit("secrets.json is not valid JSON: %s" % e)
for name, block in data.items():
    if isinstance(block, dict) and isinstance(block.get("App"), dict):
        print(name)
PY
}

# The App section of an environment's block, as JSON (empty when there is none).
app_section() {
  [ -f "$SECRETS_JSON" ] || return 0
  python3 - "$SECRETS_JSON" "$1" <<'PY'
import json, sys
try:
    data = json.load(open(sys.argv[1], encoding="utf-8-sig"))
except ValueError as e:
    sys.exit("secrets.json is not valid JSON: %s" % e)
app = (data.get(sys.argv[2]) or {}).get("App")
if isinstance(app, dict):
    print(json.dumps(app, indent=2))
PY
}

# One-line description of an environment for --list.
describe() {
  app_section "$1" | python3 -c '
import json, sys
a = json.load(sys.stdin)
if (a.get("Features") or {}).get("UseMockData"): d = "demo data, no API"
elif (a.get("Api") or {}).get("UseLocalApiInDebug"): d = "API on this machine (port '"$API_PORT"')"
else: d = (a.get("Api") or {}).get("BaseUrl", "")
if (a.get("GoogleMaps") or {}).get("ApiKey"): d += " · Google key"
print(d)'
}

list() {
  ensure_secrets_json
  echo "${bold}Environments${reset} ${dim}(blocks of secrets.json with an App section)${reset}"
  local current p; current="$(json_value Environment "$GENERATED")"
  for p in $(env_names); do
    if [ "$p" = "$current" ]; then echo "  ${green}● $p${reset}  ${dim}$(describe "$p")  (current)${reset}"
    else echo "  ○ $p  ${dim}$(describe "$p")${reset}"; fi
  done
}

show() {
  local name; name="$(json_value Environment "$GENERATED")"
  echo "${bold}App environment:${reset} ${name:-none (built-in defaults)}"
  if [ "$(setting UseMockData)" = "true" ]; then
    echo "  Data:          demo data (no API calls)"
    return
  fi
  local base local_debug; base="$(setting BaseUrl)"; local_debug="$(setting UseLocalApiInDebug)"
  if [ "$local_debug" = "true" ]; then
    echo "  Debug builds:  Android $(setting LocalBaseUrlAndroid)   iOS $(setting LocalBaseUrlApple)"
    echo "  Release builds: $base"
  else
    echo "  Debug + Release builds: $base"
  fi
  if [ -n "$(json_value ApiKey "$GENERATED")" ]; then echo "  Google Maps:   key set ${dim}(secrets.json → ${name:-?}.App)${reset}"
  else echo "  Google Maps:   no key – sample addresses"; fi
}

add_device_ip_to_android() {
  local ip="$1"
  [ -f "$NSC_FILE" ] || { warn "Android network security config not found – skipped."; return; }
  if grep -q ">$ip</domain>" "$NSC_FILE"; then ok "Android already allows http to $ip"; return; fi
  local tmp; tmp="$(mktemp)"
  awk -v ip="$ip" '/<\/domain-config>/ && !done { print "        <domain includeSubdomains=\"false\">" ip "</domain>"; done=1 } { print }' "$NSC_FILE" > "$tmp" \
    && cat "$tmp" > "$NSC_FILE" && rm -f "$tmp"
  ok "Android allows http to $ip ${dim}(network_security_config.xml – remove it before a release)${reset}"
}

use_profile() {
  local name="$1" ip="$2" section
  ensure_secrets_json
  section="$(app_section "$name")"
  if [ -z "$section" ]; then
    echo "No App section for '$name' in secrets.json."; echo; list; exit 1
  fi

  printf '%s\n' "$section" > "$GENERATED"
  if [ -n "$ip" ]; then
    [[ "$ip" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] || { rm -f "$GENERATED"; fail "--device-ip needs an IPv4 address like 192.168.1.20 (got '$ip')."; }
    [ "$(json_value UseLocalApiInDebug "$GENERATED")" = "true" ] || { rm -f "$GENERATED"; fail "--device-ip only applies to an environment that uses the local API (e.g. local)."; }
    local tmp; tmp="$(mktemp)"
    sed -e "s#\"LocalBaseUrlAndroid\"[[:space:]]*:[[:space:]]*\"[^\"]*\"#\"LocalBaseUrlAndroid\": \"http://$ip:$API_PORT/\"#" \
        -e "s#\"LocalBaseUrlApple\"[[:space:]]*:[[:space:]]*\"[^\"]*\"#\"LocalBaseUrlApple\": \"http://$ip:$API_PORT/\"#" \
        "$GENERATED" > "$tmp" && cat "$tmp" > "$GENERATED" && rm -f "$tmp"
  fi
  validate_json "$GENERATED"
  rm -f "$APP_DIR/appsettings.env.json"   # the old per-environment profile file, no longer used
  ok "Using ${bold}$name${reset} ${dim}← secrets.json → $name.App → DryCleaning/appsettings.secrets.json (git-ignored)${reset}"
  [ -n "$ip" ] && add_device_ip_to_android "$ip"

  # Sanity checks
  local mock base; mock="$(setting UseMockData)"; base="$(setting BaseUrl)"
  if [ "$name" = "production" ] && [ "$mock" = "true" ]; then warn "production uses demo data (UseMockData: true)."; fi
  if [ "$mock" != "true" ] && [ "$(setting UseLocalApiInDebug)" != "true" ]; then
    case "$base" in https://*) ;; *) warn "BaseUrl '$base' is not https – phones will refuse it.";; esac
    case "$base" in */) ;; *) warn "BaseUrl should end with '/'.";; esac
    case "$base" in */api/) warn "BaseUrl should be the server root, without 'api/'.";; esac
  fi
  if [ -z "$(json_value ApiKey "$GENERATED")" ] && [ "$mock" != "true" ]; then
    echo "  ${dim}– No Google Maps key in $name.App → GoogleMaps → ApiKey (address search uses sample addresses).${reset}"
  fi
  if [ -n "$ip" ]; then
    echo "  ${dim}Start the API so the phone can reach it:  cd DryCleaning.Api && dotnet run --launch-profile http${reset}"
  elif [ "$name" = "local" ]; then
    echo "  ${dim}Start the API first:  ./run-api.sh${reset}"
  fi
  echo
  show
  echo
  echo "${bold}Next:${reset} rebuild and run the app (settings are built into the app)."
}

# ── Arguments ─────────────────────────────────────────────────────────
PROFILE=""; DEVICE_IP=""; ACTION="use"
while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) usage; exit 0 ;;
    -s|--show) ACTION="show" ;;
    -l|--list) ACTION="list" ;;
    --reset) ACTION="reset" ;;
    --device-ip) shift; DEVICE_IP="${1:-}"; [ -n "$DEVICE_IP" ] || fail "--device-ip needs an address." ;;
    --device-ip=*) DEVICE_IP="${1#*=}" ;;
    -*) echo "Unknown option: $1"; echo; usage; exit 1 ;;
    *) [ -z "$PROFILE" ] || fail "Only one environment at a time (got '$PROFILE' and '$1')."; PROFILE="$1" ;;
  esac
  shift
done

[ -f "$BASE_FILE" ] || fail "DryCleaning/appsettings.json not found – run this script from the repository."

case "$ACTION" in
  show) show ;;
  list) list ;;
  reset)
    rm -f "$GENERATED" "$APP_DIR/appsettings.env.json"
    ok "Removed DryCleaning/appsettings.secrets.json – the app uses its built-in defaults (no environment, no keys)."
    echo; show ;;
  use)
    if [ -z "$PROFILE" ]; then usage; echo; list; exit 1; fi
    use_profile "$PROFILE" "$DEVICE_IP" ;;
esac
