#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# EDry Cleaners – switch the mobile app between environments (macOS / Linux)
#
#   ./set-app-env.sh local                 App → API on this machine (./run-api.sh)
#   ./set-app-env.sh local --device-ip 192.168.1.20
#                                          Same, for a physical phone on your Wi-Fi
#   ./set-app-env.sh mock                  Demo data, no API needed
#   ./set-app-env.sh staging               App → test server
#   ./set-app-env.sh production            App → https://edrycleaners.co.za/
#   ./set-app-env.sh --show                Which environment / server the next build uses
#   ./set-app-env.sh --list                Available environments
#   ./set-app-env.sh --reset               Back to plain DryCleaning/appsettings.json
#   ./set-app-env.sh --help                Show this help        (short forms: -s, -l, -h)
#
# Profiles live in DryCleaning/environments/<name>.json and only list what differs from
# appsettings.json. The chosen one is copied to DryCleaning/appsettings.env.json (git-ignored) and
# built into the app – rebuild/redeploy the app after switching.
# "permission denied"? Run once: chmod +x set-app-env.sh   (or use: bash set-app-env.sh)
# ─────────────────────────────────────────────────────────────────────────────
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/DryCleaning"
ENV_DIR="$APP_DIR/environments"
BASE_FILE="$APP_DIR/appsettings.json"
TARGET_FILE="$APP_DIR/appsettings.env.json"
NSC_FILE="$APP_DIR/Platforms/Android/Resources/xml/network_security_config.xml"
API_PORT=5180

bold=$'\033[1m'; dim=$'\033[2m'; green=$'\033[32m'; yellow=$'\033[33m'; red=$'\033[31m'; reset=$'\033[0m'
ok()   { echo "  ${green}✓${reset} $*"; }
warn() { echo "  ${yellow}!${reset} $*"; }
fail() { echo "  ${red}✗${reset} $*" >&2; exit 1; }

usage() { sed -n '3,20p' "$0" | sed 's/^# \{0,1\}//'; }

# Value of a "Key": value pair in a JSON file (simple string/bool values; keys are unique in our files).
json_value() {
  [ -f "$2" ] || return 0
  sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\{0,1\}\([^\",}]*\)\"\{0,1\}.*/\1/p" "$2" | head -1 | sed 's/[[:space:]]*$//'
}

# Effective value: the environment file wins over appsettings.json.
setting() {
  local v; v="$(json_value "$1" "$TARGET_FILE")"
  [ -n "$v" ] || v="$(json_value "$1" "$BASE_FILE")"
  echo "$v"
}

validate_json() {
  if command -v python3 >/dev/null 2>&1; then
    python3 -c 'import json,sys; json.load(open(sys.argv[1]))' "$1" 2>/dev/null || fail "$(basename "$1") is not valid JSON."
  elif command -v plutil >/dev/null 2>&1; then
    plutil -convert json -o /dev/null "$1" >/dev/null 2>&1 || fail "$(basename "$1") is not valid JSON."
  fi
}

profiles() { for f in "$ENV_DIR"/*.json; do [ -f "$f" ] && basename "$f" .json; done; }

list() {
  echo "${bold}Environments${reset} ${dim}(DryCleaning/environments/)${reset}"
  local current; current="$(json_value Environment "$TARGET_FILE")"
  for p in $(profiles); do
    local f="$ENV_DIR/$p.json" desc
    if [ "$(json_value UseMockData "$f")" = "true" ]; then desc="demo data, no API"
    elif [ "$(json_value UseLocalApiInDebug "$f")" = "true" ]; then desc="API on this machine (port $API_PORT)"
    else desc="$(json_value BaseUrl "$f")"; fi
    if [ "$p" = "$current" ]; then echo "  ${green}● $p${reset}  ${dim}$desc  (current)${reset}"
    else echo "  ○ $p  ${dim}$desc${reset}"; fi
  done
}

show() {
  local name; name="$(json_value Environment "$TARGET_FILE")"
  echo "${bold}App environment:${reset} ${name:-none (plain appsettings.json)}"
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
  echo "  Google Maps:   $([ -n "$(setting ApiKey)" ] && echo "key set" || echo "no key – sample addresses")"
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
  local name="$1" ip="$2" source="$ENV_DIR/$1.json"
  [ -f "$source" ] || { echo "Unknown environment '$name'."; echo; list; exit 1; }
  validate_json "$source"

  if [ -n "$ip" ]; then
    [[ "$ip" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] || fail "--device-ip needs an IPv4 address like 192.168.1.20 (got '$ip')."
    [ "$(json_value UseLocalApiInDebug "$source")" = "true" ] || fail "--device-ip only applies to an environment that uses the local API (e.g. local)."
    sed -e "s#\"LocalBaseUrlAndroid\"[[:space:]]*:[[:space:]]*\"[^\"]*\"#\"LocalBaseUrlAndroid\": \"http://$ip:$API_PORT/\"#" \
        -e "s#\"LocalBaseUrlApple\"[[:space:]]*:[[:space:]]*\"[^\"]*\"#\"LocalBaseUrlApple\": \"http://$ip:$API_PORT/\"#" \
        "$source" > "$TARGET_FILE"
  else
    cp "$source" "$TARGET_FILE"
  fi
  validate_json "$TARGET_FILE"
  ok "Using ${bold}$name${reset} ${dim}→ DryCleaning/appsettings.env.json${reset}"
  [ -n "$ip" ] && add_device_ip_to_android "$ip"

  # Sanity checks
  local mock base; mock="$(setting UseMockData)"; base="$(setting BaseUrl)"
  if [ "$name" = "production" ] && [ "$mock" = "true" ]; then warn "production uses demo data (UseMockData: true)."; fi
  if [ "$mock" != "true" ] && [ "$(setting UseLocalApiInDebug)" != "true" ]; then
    case "$base" in https://*) ;; *) warn "BaseUrl '$base' is not https – phones will refuse it.";; esac
    case "$base" in */) ;; *) warn "BaseUrl should end with '/'.";; esac
    case "$base" in */api/) warn "BaseUrl should be the server root, without 'api/'.";; esac
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

# ── Arguments ───────────────────────────────────────────────────────────────
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
    rm -f "$TARGET_FILE"
    ok "Removed DryCleaning/appsettings.env.json – the app uses appsettings.json as committed."
    echo; show ;;
  use)
    if [ -z "$PROFILE" ]; then usage; echo; list; exit 1; fi
    use_profile "$PROFILE" "$DEVICE_IP" ;;
esac
