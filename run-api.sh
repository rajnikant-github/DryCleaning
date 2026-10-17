#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# EDry Cleaners – one-command local API runner (macOS, Intel or Apple Silicon)
#
#   ./run-api.sh             Check → fix only what's missing (asks first) → run API → open Swagger
#   ./run-api.sh --check     Only run the checks, change nothing
#   ./run-api.sh --yes       Fix what's missing without asking
#   ./run-api.sh --reset-db  Recreate the SQL Server container (wipes local data)
#   ./run-api.sh --stop      Stop the SQL Server container
#   ./run-api.sh --help      Show this help        (short forms: -h, -y)
#
# Options can be combined, e.g.  ./run-api.sh --reset-db --yes
# "permission denied"? Run once: chmod +x run-api.sh   (or use: bash run-api.sh)
# Works from any folder, e.g.    ~/Repos/DryCleaning/run-api.sh
# Ctrl+C stops the API. SQL Server keeps running in Docker for the next run.
# ─────────────────────────────────────────────────────────────────────────────
set -uo pipefail

# Settings – must match ConnectionStrings:DefaultConnection in DryCleaning.Api/appsettings.json
CONTAINER="edry-sql"
SA_PASSWORD='YourStrong!Passw0rd'
SQL_PORT=1433
SQL_IMAGE="mcr.microsoft.com/mssql/server:2022-latest"
DOTNET_MAJOR=10
# Named migrations, oldest first. A missing one is created (only if the model actually needs it).
MIGRATIONS=(AddPickupSlotAndItemNotes AddPushDevicesAndRefreshTokens ClientGeneratedIds AddUserPhoto)
SWAGGER_URL="https://localhost:7180/swagger"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$SCRIPT_DIR/DryCleaning.Api"
export PATH="$PATH:$HOME/.dotnet/tools"

# ── Output helpers ──────────────────────────────────────────────────────────
bold=$'\033[1m'; dim=$'\033[2m'; green=$'\033[32m'; yellow=$'\033[33m'; red=$'\033[31m'; reset=$'\033[0m'
section() { echo; echo "${bold}$*${reset}"; }
pass()    { printf "  ${green}✓${reset} %-34s ${dim}%s${reset}\n" "$1" "${2:-}"; }
todo()    { printf "  ${yellow}✗${reset} %-34s ${yellow}%s${reset}\n" "$1" "$2"; }
block()   { printf "  ${red}✗${reset} %-34s ${red}%s${reset}\n" "$1" "$2"; }
info()    { printf "  ${dim}–${reset} %-34s ${dim}%s${reset}\n" "$1" "$2"; }
step()    { echo; echo "${bold}▶ $*${reset}"; }
ok()      { echo "  ${green}✓${reset} $*"; }
fail()    { echo; echo "  ${red}✗ $1${reset}"; shift; [ $# -gt 0 ] && printf '    %s\n' "$@"; echo; exit 1; }

usage() { sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'; exit 0; }

MODE="run"; ASSUME_YES=false; RESET_DB=false
for arg in "$@"; do
  case "$arg" in
    --help|-h)  usage ;;
    --check)    MODE="check" ;;
    --yes|-y)   ASSUME_YES=true ;;
    --reset-db) RESET_DB=true ;;
    --stop)     docker stop "$CONTAINER" >/dev/null 2>&1 && echo "Stopped $CONTAINER." || echo "$CONTAINER is not running."; exit 0 ;;
    *)          echo "Unknown option: $arg"; usage ;;
  esac
done

[ -d "$API_DIR" ] || fail "Can't find DryCleaning.Api next to this script." "Put run-api.sh in the repo root (the folder with DryCleaning.sln)."

# ═════════════════════════════════════════════════════════════════════════════
# PHASE 1 – CHECK (read-only: nothing is installed, started or changed here)
# ═════════════════════════════════════════════════════════════════════════════
BLOCKERS=()   # things the user must install themselves
ACTIONS=()    # things this script will do (after confirmation)

container_exists()  { docker ps -a --format '{{.Names}}' 2>/dev/null | grep -qx "$CONTAINER"; }
container_running() { [ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER" 2>/dev/null)" = "true" ]; }
find_sqlcmd() {
  for c in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
    docker exec "$CONTAINER" test -x "$c" 2>/dev/null && { echo "$c"; return; }
  done
}
sql_login() {   # 0 = login ok, 1 = not ready / unknown, 2 = wrong password
  local cmd out; cmd=$(find_sqlcmd)
  if [ -z "$cmd" ]; then
    docker logs "$CONTAINER" 2>&1 | grep -q "SQL Server is now ready for client connections" && return 0 || return 1
  fi
  out=$(docker exec "$CONTAINER" "$cmd" -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" 2>&1) && return 0
  echo "$out" | grep -q "Login failed for user 'sa'" && return 2
  return 1
}
port_in_use() { lsof -nP -iTCP:"$SQL_PORT" -sTCP:LISTEN >/dev/null 2>&1; }
migration_exists() { find "$API_DIR" \( -name bin -o -name obj \) -prune -o -name "*_$1.cs" -print 2>/dev/null | grep -q .; }

section "Checking your setup (no changes yet)"

# Docker
DOCKER_INSTALLED=false; DOCKER_RUNNING=false
if command -v docker >/dev/null 2>&1; then
  DOCKER_INSTALLED=true
  pass "Docker Desktop installed" "$(docker --version 2>/dev/null | cut -d, -f1)"
  if docker info >/dev/null 2>&1; then
    DOCKER_RUNNING=true; pass "Docker running"
  else
    todo "Docker running" "not running → will start Docker Desktop"; ACTIONS+=("Start Docker Desktop")
  fi
else
  block "Docker Desktop installed" "missing"
  BLOCKERS+=("Install Docker Desktop: https://www.docker.com/products/docker-desktop/")
fi

# SQL Server container
if $DOCKER_INSTALLED; then
  if ! $DOCKER_RUNNING; then
    info "SQL Server container" "checked after Docker starts"
  elif $RESET_DB && container_exists; then
    todo "SQL Server container" "--reset-db → will recreate (local data is wiped)"; ACTIONS+=("Recreate SQL Server container '$CONTAINER' (wipes local data)")
  elif ! container_exists; then
    if port_in_use; then
      block "SQL Server container" "port $SQL_PORT is used by another program"
      BLOCKERS+=("Free port $SQL_PORT – see what uses it: lsof -nP -iTCP:$SQL_PORT -sTCP:LISTEN")
    else
      todo "SQL Server container" "missing → will create '$CONTAINER' (~1.5 GB download first time)"
      ACTIONS+=("Create SQL Server container '$CONTAINER'")
    fi
  elif ! container_running; then
    todo "SQL Server container" "stopped → will start it"; ACTIONS+=("Start SQL Server container '$CONTAINER'")
  else
    sql_login; rc=$?
    case $rc in
      0) pass "SQL Server container" "running, login OK on localhost:$SQL_PORT" ;;
      2) todo "SQL Server container" "wrong SA password → will recreate (local data is wiped)"
         ACTIONS+=("Recreate SQL Server container '$CONTAINER' (wipes local data)"); RESET_DB=true ;;
      *) pass "SQL Server container" "running (still starting up – will wait)" ;;
    esac
  fi
fi

# .NET SDK
DOTNET_OK=false
if command -v dotnet >/dev/null 2>&1; then
  if dotnet --list-sdks 2>/dev/null | grep -q "^${DOTNET_MAJOR}\."; then
    DOTNET_OK=true
    pass ".NET $DOTNET_MAJOR SDK" "$(dotnet --list-sdks | grep "^${DOTNET_MAJOR}\." | tail -1 | cut -d' ' -f1)"
  else
    block ".NET $DOTNET_MAJOR SDK" "missing (found: $(dotnet --list-sdks | tail -1 | cut -d' ' -f1))"
    BLOCKERS+=("Install the .NET $DOTNET_MAJOR SDK: https://dotnet.microsoft.com/download/dotnet/$DOTNET_MAJOR.0")
  fi
else
  block ".NET $DOTNET_MAJOR SDK" "missing"
  BLOCKERS+=("Install the .NET $DOTNET_MAJOR SDK: https://dotnet.microsoft.com/download/dotnet/$DOTNET_MAJOR.0")
fi

# dotnet-ef, dev certificate, migration
if $DOTNET_OK; then
  if dotnet ef --version >/dev/null 2>&1; then
    pass "dotnet-ef tool" "$(dotnet ef --version 2>/dev/null | tail -1)"
  else
    todo "dotnet-ef tool" "missing → will install"; ACTIONS+=("Install dotnet-ef (global tool)")
  fi

  if dotnet dev-certs https --check --trust >/dev/null 2>&1; then
    pass "HTTPS dev certificate" "trusted"
  else
    todo "HTTPS dev certificate" "not trusted → will trust (macOS may ask your password)"; ACTIONS+=("Trust the HTTPS development certificate")
  fi

  for m in "${MIGRATIONS[@]}"; do
    if migration_exists "$m"; then
      pass "Migration $m" "present"
    else
      todo "Migration $m" "missing → will create"; ACTIONS+=("Create migration $m")
    fi
  done
  info "Other pending model changes" "checked after the build"
fi

# ── Summary ─────────────────────────────────────────────────────────────────
if [ ${#BLOCKERS[@]} -gt 0 ]; then
  section "${red}Please install these first, then run ./run-api.sh again:${reset}"
  for b in "${BLOCKERS[@]}"; do echo "  • $b"; done
  echo; exit 1
fi

if [ ${#ACTIONS[@]} -eq 0 ]; then
  section "${green}Everything is in place.${reset}"
else
  section "This script will:"
  for a in "${ACTIONS[@]}"; do echo "  • $a"; done
fi

if [ "$MODE" = "check" ]; then
  echo; echo "Check only – nothing was changed. Run ./run-api.sh to apply and start the API."; exit 0
fi

if [ ${#ACTIONS[@]} -gt 0 ] && ! $ASSUME_YES; then
  echo
  read -r -p "Continue? [Y/n] " answer
  case "${answer:-Y}" in [Yy]*) ;; *) echo "Nothing changed."; exit 0 ;; esac
fi

# ═════════════════════════════════════════════════════════════════════════════
# PHASE 2 – FIX only what was missing
# ═════════════════════════════════════════════════════════════════════════════
PLATFORM_ARGS=()
[ "$(uname -m)" = "arm64" ] && PLATFORM_ARGS=(--platform linux/amd64)   # SQL Server ships amd64 only – runs via Rosetta

if ! $DOCKER_RUNNING; then
  step "Starting Docker Desktop"
  open -a Docker 2>/dev/null || fail "Couldn't start Docker Desktop." "Open it manually and run this script again."
  for _ in $(seq 1 90); do docker info >/dev/null 2>&1 && break; sleep 2; done
  docker info >/dev/null 2>&1 || fail "Docker didn't start within 3 minutes." "Open Docker Desktop, wait for 'Engine running', then run this script again."
  ok "Docker is running"
fi

# Container (re-evaluated now that Docker is up)
if $RESET_DB && container_exists; then
  step "Recreating SQL Server container"
  docker rm -f "$CONTAINER" >/dev/null && ok "Old container removed"
fi
if ! container_exists; then
  port_in_use && fail "Port $SQL_PORT is used by another program." "See what it is: lsof -nP -iTCP:$SQL_PORT -sTCP:LISTEN"
  step "Creating SQL Server container '$CONTAINER' (first time downloads ~1.5 GB)"
  docker run -d --name "$CONTAINER" ${PLATFORM_ARGS[@]+"${PLATFORM_ARGS[@]}"} \
    -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=$SA_PASSWORD" \
    -p "$SQL_PORT:1433" "$SQL_IMAGE" >/dev/null \
    || fail "Couldn't create the SQL Server container." "Check Docker Desktop has enough disk space and try again."
  ok "Container created"
elif ! container_running; then
  step "Starting SQL Server container '$CONTAINER'"
  docker start "$CONTAINER" >/dev/null || fail "Couldn't start $CONTAINER." "Try: ./run-api.sh --reset-db"
  ok "Container started"
fi

if ! dotnet ef --version >/dev/null 2>&1; then
  step "Installing dotnet-ef"
  dotnet tool install --global dotnet-ef >/dev/null || fail "Couldn't install dotnet-ef." "Run manually: dotnet tool install --global dotnet-ef"
  ok "dotnet-ef $(dotnet ef --version 2>/dev/null | tail -1)"
fi

if ! dotnet dev-certs https --check --trust >/dev/null 2>&1; then
  step "Trusting the HTTPS development certificate"
  if dotnet dev-certs https --trust >/dev/null 2>&1; then ok "Certificate trusted"
  else echo "  ${yellow}!${reset} Couldn't trust it – use http://localhost:5180/swagger instead."; fi
fi

# Wait for SQL Server (always – it may still be starting)
step "Waiting for SQL Server to accept logins"
READY=false
for i in $(seq 1 90); do
  if ! container_running; then
    echo; docker logs --tail 25 "$CONTAINER" 2>&1 | sed 's/^/    /'
    fail "SQL Server stopped while starting (logs above)." \
         "Apple Silicon: Docker Desktop → Settings → General → enable" \
         "  'Use Rosetta for x86_64/amd64 emulation on Apple Silicon', Apply & restart." \
         "Give Docker at least 4 GB memory (Settings → Resources), then run: ./run-api.sh --reset-db"
  fi
  sql_login; rc=$?
  [ $rc -eq 0 ] && { READY=true; break; }
  [ $rc -eq 2 ] && fail "SQL Server rejected the password." "Run: ./run-api.sh --reset-db"
  [ $((i % 10)) -eq 0 ] && echo "  …still starting ($((i * 2))s)"
  sleep 2
done
$READY || fail "SQL Server didn't become ready within 3 minutes." "Check the logs: docker logs $CONTAINER | tail -30"
nc -z localhost "$SQL_PORT" >/dev/null 2>&1 || fail "Port $SQL_PORT isn't reachable from your Mac." "Recreate the container: ./run-api.sh --reset-db"
ok "SQL Server ready on localhost:$SQL_PORT"

# Build + migrations
cd "$API_DIR" || exit 1
build() {
  dotnet build --nologo -v quiet >/tmp/edry-build.log 2>&1 && return 0
  grep -E "error [A-Z]+[0-9]+" /tmp/edry-build.log | sort -u | head -20 | sed 's/^/    /'
  fail "Build failed (errors above; full log: /tmp/edry-build.log)."
}
add_migration() {
  dotnet ef migrations add "$1" -o Data/Migrations --no-build >/tmp/edry-ef.log 2>&1 \
    || { tail -20 /tmp/edry-ef.log | sed 's/^/    /'; fail "Couldn't create migration $1 (log above)."; }
  ok "Migration $1 created"
  build
}

step "Building the API"
build
ok "Build succeeded"

has_pending_changes() {
  ! dotnet ef migrations has-pending-model-changes --no-build >/tmp/edry-ef.log 2>&1 && grep -qi "changes have been made" /tmp/edry-ef.log
}

for m in "${MIGRATIONS[@]}"; do
  migration_exists "$m" && continue
  if has_pending_changes; then
    step "Creating migration $m"
    add_migration "$m"
  fi
done

step "Checking for other pending model changes"
if has_pending_changes; then
  add_migration "ModelUpdate_$(date +%Y%m%d%H%M%S)"
else
  ok "None – migrations are applied automatically when the API starts"
fi

# ═════════════════════════════════════════════════════════════════════════════
# PHASE 3 – RUN
# ═════════════════════════════════════════════════════════════════════════════
step "Starting the API"
echo "  Swagger:  ${bold}$SWAGGER_URL${reset}   (http: http://localhost:5180/swagger)"
echo "  Demo logins (password Demo@12345):"
echo "    Customer  sipho@example.co.za"
echo "    Provider  thandi@sparkledrycleaners.co.za"
echo "    Driver    bongani@drivers.edrycleaners.co.za"
echo "    Admin     admin@edrycleaners.co.za"
echo "  In Swagger: POST /api/auth/login → copy accessToken → Authorize → paste."
echo "  Ctrl+C stops the API (SQL Server keeps running; ./run-api.sh --stop stops it)."
echo

( for _ in $(seq 1 60); do
    curl -sk -o /dev/null "$SWAGGER_URL/index.html" && { open "$SWAGGER_URL"; exit 0; }
    sleep 2
  done ) &
OPENER_PID=$!
trap 'kill $OPENER_PID 2>/dev/null' EXIT

dotnet run --no-build --launch-profile https
