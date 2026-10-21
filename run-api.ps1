<#
.SYNOPSIS
  EDry Cleaners - one-command local API runner for Windows (PowerShell 5.1 or 7+).

.DESCRIPTION
  Checks your setup first (changes nothing), fixes only what's missing after asking,
  then runs the API and opens Swagger.

    .\run-api.ps1             Check -> fix what's missing (asks first) -> run API -> open Swagger
    .\run-api.ps1 -Check      Only run the checks, change nothing
    .\run-api.ps1 -Yes        Fix what's missing without asking
    .\run-api.ps1 -ResetDb    Recreate the SQL Server container (wipes local data)
    .\run-api.ps1 -Stop       Stop the SQL Server container
    .\run-api.ps1 -Help       Show this help

  The macOS-style options also work: --check, --yes / -y, --reset-db, --stop, --help / -h.
  Ctrl+C stops the API. SQL Server keeps running in Docker for the next run.

  If scripts are blocked:  powershell -ExecutionPolicy Bypass -File .\run-api.ps1
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Yes,
    [switch]$ResetDb,
    [switch]$Stop,
    [switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Rest
)

$ErrorActionPreference = 'Continue'

# Settings - must match ConnectionStrings:DefaultConnection in DryCleaning.Api\appsettings.json
$Container    = 'edry-sql'
$SaPassword   = 'YourStrong!Passw0rd'
$SqlPort      = 1433
$SqlImage     = 'mcr.microsoft.com/mssql/server:2022-latest'
$DotnetMajor  = 10
# Named migrations, oldest first. A missing one is created (only if the model actually needs it).
$Migrations   = @('AddPickupSlotAndItemNotes', 'AddPushDevicesAndRefreshTokens', 'ClientGeneratedIds', 'AddUserPhoto', 'AddProviderWorkingHours', 'AddDeliveryPricing', 'AddExtraCharges', 'AddConfigurablePricing', 'AddPendingRegistrations', 'AddAccountApproval')
$SwaggerUrl   = 'https://localhost:7180/swagger'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ApiDir    = Join-Path $ScriptDir 'DryCleaning.Api'
$env:PATH  = "$env:PATH;$env:USERPROFILE\.dotnet\tools"
$LogDir    = $env:TEMP

# -- Output helpers -----------------------------------------------------------
function Section([string]$Text, [string]$Color = 'White') { Write-Host ''; Write-Host $Text -ForegroundColor $Color }
function Pass([string]$Name, [string]$Detail = '')  { Write-Host '  [ OK ] ' -ForegroundColor Green -NoNewline;  Write-Host ('{0,-34} ' -f $Name) -NoNewline; Write-Host $Detail -ForegroundColor DarkGray }
function Todo([string]$Name, [string]$Detail)       { Write-Host '  [FIX ] ' -ForegroundColor Yellow -NoNewline; Write-Host ('{0,-34} ' -f $Name) -NoNewline; Write-Host $Detail -ForegroundColor Yellow }
function Block([string]$Name, [string]$Detail)      { Write-Host '  [NEED] ' -ForegroundColor Red -NoNewline;    Write-Host ('{0,-34} ' -f $Name) -NoNewline; Write-Host $Detail -ForegroundColor Red }
function Info([string]$Name, [string]$Detail)       { Write-Host '  [ -- ] ' -ForegroundColor DarkGray -NoNewline; Write-Host ('{0,-34} ' -f $Name) -NoNewline; Write-Host $Detail -ForegroundColor DarkGray }
function Step([string]$Text) { Write-Host ''; Write-Host "> $Text" -ForegroundColor Cyan }
function Ok([string]$Text)   { Write-Host "  [ OK ] $Text" -ForegroundColor Green }
function Fail([string]$Text, [string[]]$Hints = @()) {
    Write-Host ''
    Write-Host "  [FAIL] $Text" -ForegroundColor Red
    foreach ($h in $Hints) { Write-Host "         $h" }
    Write-Host ''
    exit 1
}
function Show-Help {
    Write-Host @"
EDry Cleaners - one-command local API runner (Windows)

  .\run-api.ps1             Check -> fix what's missing (asks first) -> run API -> open Swagger
  .\run-api.ps1 -Check      Only run the checks, change nothing
  .\run-api.ps1 -Yes        Fix what's missing without asking
  .\run-api.ps1 -ResetDb    Recreate the SQL Server container (wipes local data)
  .\run-api.ps1 -Stop       Stop the SQL Server container
  .\run-api.ps1 -Help       Show this help

  Options can be combined, e.g.  .\run-api.ps1 -ResetDb -Yes
  macOS-style options also work: --check, --yes / -y, --reset-db, --stop, --help / -h
  If scripts are blocked:        powershell -ExecutionPolicy Bypass -File .\run-api.ps1

Ctrl+C stops the API. SQL Server keeps running in Docker for the next run.
"@
    exit 0
}

# macOS-style arguments (--check, -y, ...) for people used to run-api.sh
foreach ($arg in @($Rest)) {
    if (-not $arg) { continue }
    switch ($arg.ToLowerInvariant()) {
        '--check'    { $Check = $true }
        '--yes'      { $Yes = $true }
        '-y'         { $Yes = $true }
        '--reset-db' { $ResetDb = $true }
        '--stop'     { $Stop = $true }
        '--help'     { $Help = $true }
        '-h'         { $Help = $true }
        default      { Write-Host "Unknown option: $arg"; Show-Help }
    }
}
if ($Help) { Show-Help }

function Test-Command([string]$Name) { [bool](Get-Command $Name -ErrorAction SilentlyContinue) }

if ($Stop) {
    if (Test-Command 'docker') { docker stop $Container *> $null }
    if ($LASTEXITCODE -eq 0) { Write-Host "Stopped $Container." } else { Write-Host "$Container is not running." }
    exit 0
}

if (-not (Test-Path $ApiDir)) {
    Fail "Can't find DryCleaning.Api next to this script." @('Put run-api.ps1 in the repo root (the folder with DryCleaning.sln).')
}

# -- Docker / SQL helpers ------------------------------------------------------
function Test-DockerRunning { docker info *> $null; return ($LASTEXITCODE -eq 0) }
function Test-ContainerExists {
    $names = docker ps -a --format '{{.Names}}' 2>$null
    return [bool](@($names) -contains $Container)
}
function Test-ContainerRunning { return ((docker inspect -f '{{.State.Running}}' $Container 2>$null) -eq 'true') }
function Get-SqlCmdPath {
    foreach ($c in @('/opt/mssql-tools18/bin/sqlcmd', '/opt/mssql-tools/bin/sqlcmd')) {
        docker exec $Container test -x $c *> $null
        if ($LASTEXITCODE -eq 0) { return $c }
    }
    return $null
}
# 0 = login OK, 1 = not ready / unknown, 2 = wrong password
function Test-SqlLogin {
    $cmd = Get-SqlCmdPath
    if (-not $cmd) {
        $logs = docker logs $Container 2>&1 | Out-String
        if ($logs -match 'SQL Server is now ready for client connections') { return 0 } else { return 1 }
    }
    $out = docker exec $Container $cmd -S localhost -U sa -P $SaPassword -C -Q 'SELECT 1' 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) { return 0 }
    if ($out -match "Login failed for user 'sa'") { return 2 }
    return 1
}
function Test-PortInUse {
    try {
        return [bool](Get-NetTCPConnection -LocalPort $SqlPort -State Listen -ErrorAction Stop)
    } catch {
        return [bool](netstat -ano | Select-String -Pattern "LISTENING" | Select-String -Pattern ":$SqlPort\s")
    }
}
function Test-PortReachable {
    $client = New-Object System.Net.Sockets.TcpClient
    try { $client.Connect('localhost', $SqlPort); return $true } catch { return $false } finally { $client.Dispose() }
}
function Get-DockerDesktopExe {
    foreach ($p in @("$env:ProgramFiles\Docker\Docker\Docker Desktop.exe", "$env:LOCALAPPDATA\Docker\Docker Desktop.exe")) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

# =============================================================================
# PHASE 1 - CHECK (read-only: nothing is installed, started or changed here)
# =============================================================================
$Blockers = New-Object System.Collections.Generic.List[string]
$Actions  = New-Object System.Collections.Generic.List[string]

Section 'Checking your setup (no changes yet)'

# Docker
$DockerInstalled = Test-Command 'docker'
$DockerRunning = $false
if ($DockerInstalled) {
    Pass 'Docker Desktop installed' (((docker --version 2>$null) -split ',')[0])
    if (Test-DockerRunning) {
        $DockerRunning = $true
        $osType = docker info --format '{{.OSType}}' 2>$null
        if ($osType -eq 'linux') {
            Pass 'Docker running' 'Linux containers'
        } else {
            Block 'Docker running' 'Windows containers mode'
            $Blockers.Add('Switch Docker Desktop to Linux containers: right-click the Docker tray icon -> "Switch to Linux containers..."')
        }
    } elseif (Get-DockerDesktopExe) {
        Todo 'Docker running' 'not running -> will start Docker Desktop'
        $Actions.Add('Start Docker Desktop')
    } else {
        Block 'Docker running' 'not running and Docker Desktop.exe not found'
        $Blockers.Add('Start Docker Desktop manually, wait for "Engine running", then run this script again.')
    }
} else {
    Block 'Docker Desktop installed' 'missing'
    $Blockers.Add('Install Docker Desktop (with WSL 2): https://www.docker.com/products/docker-desktop/')
}

# SQL Server container
if ($DockerInstalled) {
    if (-not $DockerRunning) {
        Info 'SQL Server container' 'checked after Docker starts'
    } elseif ($ResetDb -and (Test-ContainerExists)) {
        Todo 'SQL Server container' '-ResetDb -> will recreate (local data is wiped)'
        $Actions.Add("Recreate SQL Server container '$Container' (wipes local data)")
    } elseif (-not (Test-ContainerExists)) {
        if (Test-PortInUse) {
            Block 'SQL Server container' "port $SqlPort is used by another program"
            $Blockers.Add("Free port $SqlPort (e.g. stop a local SQL Server service) - see: netstat -ano | findstr :$SqlPort")
        } else {
            Todo 'SQL Server container' "missing -> will create '$Container' (~1.5 GB download first time)"
            $Actions.Add("Create SQL Server container '$Container'")
        }
    } elseif (-not (Test-ContainerRunning)) {
        Todo 'SQL Server container' 'stopped -> will start it'
        $Actions.Add("Start SQL Server container '$Container'")
    } else {
        switch (Test-SqlLogin) {
            0 { Pass 'SQL Server container' "running, login OK on localhost:$SqlPort" }
            2 { Todo 'SQL Server container' 'wrong SA password -> will recreate (local data is wiped)'
                $Actions.Add("Recreate SQL Server container '$Container' (wipes local data)"); $ResetDb = $true }
            default { Pass 'SQL Server container' 'running (still starting up - will wait)' }
        }
    }
}

# .NET SDK
$DotnetOk = $false
if (Test-Command 'dotnet') {
    $sdks = @(dotnet --list-sdks 2>$null)
    $match = @($sdks | Where-Object { $_ -match "^$DotnetMajor\." })
    if ($match.Count -gt 0) {
        $DotnetOk = $true
        Pass ".NET $DotnetMajor SDK" (($match[-1] -split ' ')[0])
    } else {
        $found = if ($sdks.Count -gt 0) { ($sdks[-1] -split ' ')[0] } else { 'none' }
        Block ".NET $DotnetMajor SDK" "missing (found: $found)"
        $Blockers.Add("Install the .NET $DotnetMajor SDK: https://dotnet.microsoft.com/download/dotnet/$DotnetMajor.0")
    }
} else {
    Block ".NET $DotnetMajor SDK" 'missing'
    $Blockers.Add("Install the .NET $DotnetMajor SDK: https://dotnet.microsoft.com/download/dotnet/$DotnetMajor.0")
}

# dotnet-ef, dev certificate, migration
function Test-DotnetEf { dotnet ef --version *> $null; return ($LASTEXITCODE -eq 0) }
function Test-DevCert  { dotnet dev-certs https --check --trust *> $null; return ($LASTEXITCODE -eq 0) }
function Test-Migration([string]$Name) {
    return [bool](Get-ChildItem -Path $ApiDir -Recurse -Filter "*_$Name.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' })
}

if ($DotnetOk) {
    if (Test-DotnetEf) { Pass 'dotnet-ef tool' ((dotnet ef --version 2>$null | Select-Object -Last 1)) }
    else { Todo 'dotnet-ef tool' 'missing -> will install'; $Actions.Add('Install dotnet-ef (global tool)') }

    if (Test-DevCert) { Pass 'HTTPS dev certificate' 'trusted' }
    else { Todo 'HTTPS dev certificate' 'not trusted -> will trust (Windows asks to confirm)'; $Actions.Add('Trust the HTTPS development certificate') }

    foreach ($m in $Migrations) {
        if (Test-Migration $m) { Pass "Migration $m" 'present' }
        else { Todo "Migration $m" 'missing -> will create'; $Actions.Add("Create migration $m") }
    }

    Info 'Other pending model changes' 'checked after the build'
}

# -- Summary --------------------------------------------------------------------
if ($Blockers.Count -gt 0) {
    Section 'Please fix these first, then run .\run-api.ps1 again:' 'Red'
    foreach ($b in $Blockers) { Write-Host "  * $b" }
    Write-Host ''
    exit 1
}

if ($Actions.Count -eq 0) { Section 'Everything is in place.' 'Green' }
else {
    Section 'This script will:'
    foreach ($a in $Actions) { Write-Host "  * $a" }
}

if ($Check) {
    Write-Host ''
    Write-Host 'Check only - nothing was changed. Run .\run-api.ps1 to apply and start the API.'
    exit 0
}

if ($Actions.Count -gt 0 -and -not $Yes) {
    Write-Host ''
    $answer = Read-Host 'Continue? [Y/n]'
    if ($answer -and $answer -notmatch '^[Yy]') { Write-Host 'Nothing changed.'; exit 0 }
}

# =============================================================================
# PHASE 2 - FIX only what was missing
# =============================================================================
$PlatformArgs = @()
if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { $PlatformArgs = @('--platform', 'linux/amd64') }   # SQL Server ships amd64 only

if (-not $DockerRunning) {
    Step 'Starting Docker Desktop'
    Start-Process -FilePath (Get-DockerDesktopExe) | Out-Null
    for ($i = 0; $i -lt 90; $i++) { if (Test-DockerRunning) { break }; Start-Sleep -Seconds 2 }
    if (-not (Test-DockerRunning)) { Fail "Docker didn't start within 3 minutes." @('Open Docker Desktop, wait for "Engine running", then run this script again.') }
    if ((docker info --format '{{.OSType}}' 2>$null) -ne 'linux') {
        Fail 'Docker is in Windows containers mode.' @('Right-click the Docker tray icon -> "Switch to Linux containers...", then run this script again.')
    }
    Ok 'Docker is running'
}

if ($ResetDb -and (Test-ContainerExists)) {
    Step 'Recreating SQL Server container'
    docker rm -f $Container *> $null
    Ok 'Old container removed'
}
if (-not (Test-ContainerExists)) {
    if (Test-PortInUse) { Fail "Port $SqlPort is used by another program." @("See what it is: netstat -ano | findstr :$SqlPort") }
    Step "Creating SQL Server container '$Container' (first time downloads ~1.5 GB)"
    $runArgs = @('run', '-d', '--name', $Container) + $PlatformArgs + @('-e', 'ACCEPT_EULA=Y', '-e', "MSSQL_SA_PASSWORD=$SaPassword", '-p', "${SqlPort}:1433", $SqlImage)
    & docker @runArgs | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "Couldn't create the SQL Server container." @('Check Docker Desktop has enough disk space and try again.') }
    Ok 'Container created'
} elseif (-not (Test-ContainerRunning)) {
    Step "Starting SQL Server container '$Container'"
    docker start $Container *> $null
    if ($LASTEXITCODE -ne 0) { Fail "Couldn't start $Container." @('Try: .\run-api.ps1 -ResetDb') }
    Ok 'Container started'
}

if (-not (Test-DotnetEf)) {
    Step 'Installing dotnet-ef'
    dotnet tool install --global dotnet-ef *> $null
    if ($LASTEXITCODE -ne 0) { Fail "Couldn't install dotnet-ef." @('Run manually: dotnet tool install --global dotnet-ef') }
    Ok ('dotnet-ef ' + (dotnet ef --version 2>$null | Select-Object -Last 1))
}

if (-not (Test-DevCert)) {
    Step 'Trusting the HTTPS development certificate (confirm the Windows prompt)'
    dotnet dev-certs https --trust *> $null
    if ($LASTEXITCODE -eq 0) { Ok 'Certificate trusted' }
    else { Write-Host '  [WARN] Couldn''t trust it - use http://localhost:5180/swagger instead.' -ForegroundColor Yellow }
}

# Wait for SQL Server (always - it may still be starting)
Step 'Waiting for SQL Server to accept logins'
$ready = $false
for ($i = 1; $i -le 90; $i++) {
    if (-not (Test-ContainerRunning)) {
        Write-Host ''
        docker logs --tail 25 $Container 2>&1 | ForEach-Object { Write-Host "    $_" }
        Fail 'SQL Server stopped while starting (logs above).' @(
            'Give Docker at least 4 GB memory (Docker Desktop -> Settings -> Resources, or .wslconfig for WSL 2),',
            'then run: .\run-api.ps1 -ResetDb')
    }
    $rc = Test-SqlLogin
    if ($rc -eq 0) { $ready = $true; break }
    if ($rc -eq 2) { Fail 'SQL Server rejected the password.' @('Run: .\run-api.ps1 -ResetDb') }
    if ($i % 10 -eq 0) { Write-Host "  ...still starting ($($i * 2)s)" }
    Start-Sleep -Seconds 2
}
if (-not $ready) { Fail "SQL Server didn't become ready within 3 minutes." @("Check the logs: docker logs $Container --tail 30") }
if (-not (Test-PortReachable)) { Fail "Port $SqlPort isn't reachable from Windows." @('Recreate the container: .\run-api.ps1 -ResetDb') }
Ok "SQL Server ready on localhost:$SqlPort"

# Build + migrations
Set-Location $ApiDir
$buildLog = Join-Path $LogDir 'edry-build.log'
$efLog    = Join-Path $LogDir 'edry-ef.log'

function Invoke-Build {
    dotnet build --nologo -v quiet *> $buildLog
    if ($LASTEXITCODE -eq 0) { return }
    Get-Content $buildLog | Select-String -Pattern 'error [A-Z]+[0-9]+' | Select-Object -Unique -First 20 | ForEach-Object { Write-Host "    $_" }
    Fail "Build failed (errors above; full log: $buildLog)."
}
function Add-Migration([string]$Name) {
    dotnet ef migrations add $Name -o Data/Migrations --no-build *> $efLog
    if ($LASTEXITCODE -ne 0) {
        Get-Content $efLog -Tail 20 | ForEach-Object { Write-Host "    $_" }
        Fail "Couldn't create migration $Name (log above)."
    }
    Ok "Migration $Name created"
    Invoke-Build
}

Step 'Building the API'
Invoke-Build
Ok 'Build succeeded'

function Test-PendingChanges {
    dotnet ef migrations has-pending-model-changes --no-build *> $efLog
    return ($LASTEXITCODE -ne 0 -and ((Get-Content $efLog -Raw) -match 'changes have been made'))
}

foreach ($m in $Migrations) {
    if (Test-Migration $m) { continue }
    if (Test-PendingChanges) {
        Step "Creating migration $m"
        Add-Migration $m
    }
}

Step 'Checking for other pending model changes'
if (Test-PendingChanges) {
    Add-Migration ('ModelUpdate_' + (Get-Date -Format 'yyyyMMddHHmmss'))
} else {
    Ok 'None - migrations are applied automatically when the API starts'
}

# =============================================================================
# PHASE 3 - RUN
# =============================================================================
Step 'Starting the API'
Write-Host "  Swagger:  $SwaggerUrl   (http: http://localhost:5180/swagger)"
Write-Host '  Demo logins (password Demo@12345):'
Write-Host '    Customer  sipho@example.co.za'
Write-Host '    Provider  thandi@sparkledrycleaners.co.za'
Write-Host '    Driver    bongani@drivers.edrycleaners.co.za'
Write-Host '    Admin     admin@edrycleaners.co.za'
Write-Host '  In Swagger: POST /api/auth/login -> copy accessToken -> Authorize -> paste.'
Write-Host '  Ctrl+C stops the API (SQL Server keeps running; .\run-api.ps1 -Stop stops it).'
Write-Host ''

# Open Swagger once the API answers (curl.exe ships with Windows 10+).
$opener = Start-Job -ArgumentList $SwaggerUrl -ScriptBlock {
    param($url)
    for ($i = 0; $i -lt 60; $i++) {
        & curl.exe -sk -o NUL "$url/index.html" 2>$null
        if ($LASTEXITCODE -eq 0) { Start-Process $url; return }
        Start-Sleep -Seconds 2
    }
}

try {
    dotnet run --no-build --launch-profile https
} finally {
    Stop-Job $opener -ErrorAction SilentlyContinue
    Remove-Job $opener -Force -ErrorAction SilentlyContinue
}
