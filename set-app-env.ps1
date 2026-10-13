<#
EDry Cleaners - switch the mobile app between environments (Windows PowerShell 5.1+ / PowerShell 7)

  .\set-app-env.ps1 local                      App -> API on this machine (.\run-api.ps1)
  .\set-app-env.ps1 local -DeviceIp 192.168.1.20
                                               Same, for a physical phone on your Wi-Fi
  .\set-app-env.ps1 mock                       Demo data, no API needed
  .\set-app-env.ps1 staging                    App -> test server
  .\set-app-env.ps1 production                 App -> https://edrycleaners.co.za/
  .\set-app-env.ps1 -Show                      Which environment / server the next build uses
  .\set-app-env.ps1 -List                      Available environments
  .\set-app-env.ps1 -Reset                     Back to plain DryCleaning\appsettings.json
  .\set-app-env.ps1 -Help                      Show this help
  (macOS-style --show, --list, --reset, --help, --device-ip also work)

Profiles live in DryCleaning\environments\<name>.json and only list what differs from
appsettings.json. The chosen one is copied to DryCleaning\appsettings.env.json (git-ignored) and
built into the app - rebuild/redeploy the app after switching.
"running scripts is disabled"? Use: powershell -ExecutionPolicy Bypass -File .\set-app-env.ps1 local
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)][string]$Name,
    [string]$DeviceIp,
    [switch]$Show,
    [switch]$List,
    [switch]$Reset,
    [switch]$Help,
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$Rest
)

$ErrorActionPreference = 'Stop'
$ApiPort = 5180
$AppDir = Join-Path $PSScriptRoot 'DryCleaning'
$EnvDir = Join-Path $AppDir 'environments'
$BaseFile = Join-Path $AppDir 'appsettings.json'
$TargetFile = Join-Path $AppDir 'appsettings.env.json'
$NscFile = Join-Path $AppDir 'Platforms\Android\Resources\xml\network_security_config.xml'

function Ok([string]$m)   { Write-Host "  [ok] $m" -ForegroundColor Green }
function Warn([string]$m) { Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Fail([string]$m) { Write-Host "  [x]  $m" -ForegroundColor Red; exit 1 }

function Show-Usage {
    $text = Get-Content -LiteralPath $PSCommandPath -Raw
    $start = $text.IndexOf('<#') + 2
    $end = $text.IndexOf('#>')
    Write-Host $text.Substring($start, $end - $start).Trim()
}

function Read-Json([string]$path) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    try { return (Get-Content -LiteralPath $path -Raw) | ConvertFrom-Json }
    catch { Fail "$(Split-Path $path -Leaf) is not valid JSON: $($_.Exception.Message)" }
}

function Get-Value($obj, [string[]]$path) {
    $cur = $obj
    foreach ($p in $path) {
        if ($null -eq $cur) { return $null }
        $prop = $cur.PSObject.Properties[$p]
        if ($null -eq $prop) { return $null }
        $cur = $prop.Value
    }
    return $cur
}

# Effective value: the environment file wins over appsettings.json.
function Get-Setting([string[]]$path) {
    $v = Get-Value (Read-Json $TargetFile) $path
    if ($null -eq $v) { $v = Get-Value (Read-Json $BaseFile) $path }
    return $v
}

function Get-Profiles {
    if (-not (Test-Path -LiteralPath $EnvDir)) { return @() }
    return @(Get-ChildItem -LiteralPath $EnvDir -Filter '*.json' | Sort-Object Name | ForEach-Object { $_.BaseName })
}

function Show-List {
    Write-Host 'Environments (DryCleaning\environments\)'
    $current = Get-Value (Read-Json $TargetFile) @('Environment')
    foreach ($p in Get-Profiles) {
        $json = Read-Json (Join-Path $EnvDir "$p.json")
        if ((Get-Value $json @('Features', 'UseMockData')) -eq $true) { $desc = 'demo data, no API' }
        elseif ((Get-Value $json @('Api', 'UseLocalApiInDebug')) -eq $true) { $desc = "API on this machine (port $ApiPort)" }
        else { $desc = Get-Value $json @('Api', 'BaseUrl') }
        if ($p -eq $current) { Write-Host "  * $p  $desc  (current)" -ForegroundColor Green }
        else { Write-Host "    $p  $desc" }
    }
}

function Show-Current {
    $name = Get-Value (Read-Json $TargetFile) @('Environment')
    if (-not $name) { $name = 'none (plain appsettings.json)' }
    Write-Host "App environment: $name"
    if ((Get-Setting @('Features', 'UseMockData')) -eq $true) {
        Write-Host '  Data:          demo data (no API calls)'
        return
    }
    $base = Get-Setting @('Api', 'BaseUrl')
    if ((Get-Setting @('Api', 'UseLocalApiInDebug')) -eq $true) {
        Write-Host "  Debug builds:  Android $(Get-Setting @('Api', 'LocalBaseUrlAndroid'))   iOS $(Get-Setting @('Api', 'LocalBaseUrlApple'))"
        Write-Host "  Release builds: $base"
    }
    else {
        Write-Host "  Debug + Release builds: $base"
    }
    $key = Get-Setting @('GoogleMaps', 'ApiKey')
    if ($key) { Write-Host '  Google Maps:   key set' } else { Write-Host '  Google Maps:   no key - sample addresses' }
}

function Write-Utf8NoBom([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding($false)))
}

function Add-DeviceIpToAndroid([string]$ip) {
    if (-not (Test-Path -LiteralPath $NscFile)) { Warn 'Android network security config not found - skipped.'; return }
    $xml = [System.IO.File]::ReadAllText($NscFile)
    if ($xml.Contains(">$ip</domain>")) { Ok "Android already allows http to $ip"; return }
    $line = "        <domain includeSubdomains=""false"">$ip</domain>"
    $index = $xml.IndexOf('</domain-config>')
    if ($index -lt 0) { Warn 'No <domain-config> in network_security_config.xml - skipped.'; return }
    $lineStart = $xml.LastIndexOf("`n", $index) + 1
    $newline = if ($xml.Contains("`r`n")) { "`r`n" } else { "`n" }
    Write-Utf8NoBom $NscFile ($xml.Insert($lineStart, $line + $newline))
    Ok "Android allows http to $ip (network_security_config.xml - remove it before a release)"
}

function Use-Profile([string]$name, [string]$ip) {
    $source = Join-Path $EnvDir "$name.json"
    if (-not (Test-Path -LiteralPath $source)) {
        Write-Host "Unknown environment '$name'."
        Write-Host ''
        Show-List
        exit 1
    }
    $json = Read-Json $source
    $text = [System.IO.File]::ReadAllText($source)

    if ($ip) {
        if ($ip -notmatch '^(\d{1,3}\.){3}\d{1,3}$') { Fail "-DeviceIp needs an IPv4 address like 192.168.1.20 (got '$ip')." }
        if ((Get-Value $json @('Api', 'UseLocalApiInDebug')) -ne $true) { Fail '-DeviceIp only applies to an environment that uses the local API (e.g. local).' }
        $url = "http://${ip}:$ApiPort/"
        $text = [regex]::Replace($text, '"LocalBaseUrlAndroid"\s*:\s*"[^"]*"', """LocalBaseUrlAndroid"": ""$url""")
        $text = [regex]::Replace($text, '"LocalBaseUrlApple"\s*:\s*"[^"]*"', """LocalBaseUrlApple"": ""$url""")
    }

    Write-Utf8NoBom $TargetFile $text
    $null = Read-Json $TargetFile
    Ok "Using $name -> DryCleaning\appsettings.env.json"
    if ($ip) { Add-DeviceIpToAndroid $ip }

    # Sanity checks
    $mock = (Get-Setting @('Features', 'UseMockData')) -eq $true
    $base = [string](Get-Setting @('Api', 'BaseUrl'))
    if ($name -eq 'production' -and $mock) { Warn 'production uses demo data (UseMockData: true).' }
    if (-not $mock -and (Get-Setting @('Api', 'UseLocalApiInDebug')) -ne $true) {
        if (-not $base.StartsWith('https://')) { Warn "BaseUrl '$base' is not https - phones will refuse it." }
        if (-not $base.EndsWith('/')) { Warn "BaseUrl should end with '/'." }
        if ($base.EndsWith('/api/')) { Warn "BaseUrl should be the server root, without 'api/'." }
    }
    if ($ip) { Write-Host '  Start the API so the phone can reach it:  cd DryCleaning.Api; dotnet run --launch-profile http' }
    elseif ($name -eq 'local') { Write-Host '  Start the API first:  .\run-api.ps1' }
    Write-Host ''
    Show-Current
    Write-Host ''
    Write-Host 'Next: rebuild and run the app (settings are built into the app).'
}

# macOS-style arguments (--show, --device-ip 1.2.3.4, ...)
$all = @()
if ($Name) { $all += $Name }
if ($Rest) { $all += $Rest }
$Name = $null
for ($i = 0; $i -lt $all.Count; $i++) {
    $a = $all[$i]
    switch -Regex ($a) {
        '^(-h|--help)$' { $Help = $true; continue }
        '^(-s|--show)$' { $Show = $true; continue }
        '^(-l|--list)$' { $List = $true; continue }
        '^--reset$' { $Reset = $true; continue }
        '^--device-ip=' { $DeviceIp = $a.Substring(12); continue }
        '^--device-ip$' { $i++; if ($i -lt $all.Count) { $DeviceIp = $all[$i] }; continue }
        '^-' { Write-Host "Unknown option: $a"; Show-Usage; exit 1 }
        default {
            if ($Name) { Fail "Only one environment at a time (got '$Name' and '$a')." }
            $Name = $a
        }
    }
}

if (-not (Test-Path -LiteralPath $BaseFile)) { Fail 'DryCleaning\appsettings.json not found - run this script from the repository.' }

if ($Help) { Show-Usage; exit 0 }
if ($Show) { Show-Current; exit 0 }
if ($List) { Show-List; exit 0 }
if ($Reset) {
    if (Test-Path -LiteralPath $TargetFile) { Remove-Item -LiteralPath $TargetFile }
    Ok 'Removed DryCleaning\appsettings.env.json - the app uses appsettings.json as committed.'
    Write-Host ''
    Show-Current
    exit 0
}
if (-not $Name) { Show-Usage; Write-Host ''; Show-List; exit 1 }
Use-Profile $Name $DeviceIp
