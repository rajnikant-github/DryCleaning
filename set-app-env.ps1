<#
EDry Cleaners - switch the mobile app between environments (Windows PowerShell 5.1+ / PowerShell 7)

  .\set-app-env.ps1 local                      App -> API on this machine (.\run-api.ps1)
  .\set-app-env.ps1 local -DeviceIp 192.168.1.20
                                               Same, for a physical phone on your Wi-Fi
  .\set-app-env.ps1 development                App -> the shared test server
  .\set-app-env.ps1 production                 App -> https://edrycleaners.co.za/
  .\set-app-env.ps1 -Show                      Which environment / server the next build uses
  .\set-app-env.ps1 -List                      Available environments
  .\set-app-env.ps1 -Reset                     Back to the built-in defaults (no environment)
  .\set-app-env.ps1 -Help                      Show this help
  (macOS-style --show, --list, --reset, --help, --device-ip also work)

ONE file holds every setting for every environment: the repository's git-ignored secrets.json (copy
secrets.example.json to create it). Each environment is a block ("local", "development", "production") and its
"App" section is the mobile app's settings - API address, features, pricing, the Google Maps key. This script copies
that App section into DryCleaning\appsettings.secrets.json (git-ignored), which is built into the app - rebuild and
redeploy the app after switching. Only the App section goes into the app (the Api and Admin sections never do).
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
$BaseFile = Join-Path $AppDir 'appsettings.json'
$OldEnvFile = Join-Path $AppDir 'appsettings.env.json'     # the old per-environment profile file, no longer used
$ExampleJson = Join-Path $PSScriptRoot 'secrets.example.json'
$SecretsFile = Join-Path $AppDir 'appsettings.secrets.json'   # generated - the App section of the chosen environment
$SecretsJson = Join-Path $PSScriptRoot 'secrets.json'          # the single file with every setting (git-ignored)
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

# Effective value: the generated App section first, then the built-in defaults (same order as the app).
function Get-Setting([string[]]$path) {
    $v = Get-Value (Read-Json $SecretsFile) $path
    if ($null -eq $v -or $v -eq '') { $v = Get-Value (Read-Json $BaseFile) $path }
    return $v
}

function Test-Key([string]$path) { return [bool](Get-Value (Read-Json $path) @('GoogleMaps', 'ApiKey')) }

# The "App" section of an environment's block in secrets.json (null when there is none).
function Get-AppSecrets([string]$name) {
    $root = Read-Json $SecretsJson
    if ($null -eq $root) { return $null }
    return Get-Value $root @($name, 'App')
}

function Test-AppKey([string]$name) { return [bool](Get-Value (Get-AppSecrets $name) @('GoogleMaps', 'ApiKey')) }

# secrets.json is the one file for everything: create it from the template when it is missing.
function Initialize-SecretsJson {
    if (Test-Path -LiteralPath $SecretsJson) { return }
    if (-not (Test-Path -LiteralPath $ExampleJson)) { Fail 'secrets.json and secrets.example.json are both missing - run this script from the repository.' }
    Copy-Item -LiteralPath $ExampleJson -Destination $SecretsJson
    Warn 'secrets.json did not exist - created it from secrets.example.json. Fill in your keys there (git-ignored).'
}

# Environments = the blocks of secrets.json that have an App section.
function Get-Profiles {
    $root = Read-Json $SecretsJson
    if ($null -eq $root) { return @() }
    return @($root.PSObject.Properties | Where-Object { $null -ne (Get-Value $_.Value @('App')) } | ForEach-Object { $_.Name })
}

function Show-List {
    Initialize-SecretsJson
    Write-Host 'Environments (blocks of secrets.json with an App section)'
    $current = Get-Value (Read-Json $SecretsFile) @('Environment')
    foreach ($p in Get-Profiles) {
        $app = Get-AppSecrets $p
        if ((Get-Value $app @('Features', 'UseMockData')) -eq $true) { $desc = 'demo data, no API' }
        elseif ((Get-Value $app @('Api', 'UseLocalApiInDebug')) -eq $true) { $desc = "API on this machine (port $ApiPort)" }
        else { $desc = Get-Value $app @('Api', 'BaseUrl') }
        if (Test-AppKey $p) { $desc = "$desc - Google key" }
        if ($p -eq $current) { Write-Host "  * $p  $desc  (current)" -ForegroundColor Green }
        else { Write-Host "    $p  $desc" }
    }
}

function Show-Current {
    $name = Get-Value (Read-Json $SecretsFile) @('Environment')
    if (-not $name) { $name = 'none (built-in defaults)' }
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
    if (Test-Key $SecretsFile) { Write-Host "  Google Maps:   key set (secrets.json: $name.App)" }
    else { Write-Host '  Google Maps:   no key - sample addresses' }
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
    Initialize-SecretsJson
    $app = Get-AppSecrets $name
    if ($null -eq $app) {
        Write-Host "No App section for '$name' in secrets.json."
        Write-Host ''
        Show-List
        exit 1
    }
    $text = ($app | ConvertTo-Json -Depth 10) + "`n"

    if ($ip) {
        if ($ip -notmatch '^(\d{1,3}\.){3}\d{1,3}$') { Fail "-DeviceIp needs an IPv4 address like 192.168.1.20 (got '$ip')." }
        if ((Get-Value $app @('Api', 'UseLocalApiInDebug')) -ne $true) { Fail '-DeviceIp only applies to an environment that uses the local API (e.g. local).' }
        $url = "http://${ip}:$ApiPort/"
        $text = [regex]::Replace($text, '"LocalBaseUrlAndroid"\s*:\s*"[^"]*"', """LocalBaseUrlAndroid"": ""$url""")
        $text = [regex]::Replace($text, '"LocalBaseUrlApple"\s*:\s*"[^"]*"', """LocalBaseUrlApple"": ""$url""")
    }

    Write-Utf8NoBom $SecretsFile $text
    $null = Read-Json $SecretsFile
    if (Test-Path -LiteralPath $OldEnvFile) { Remove-Item -LiteralPath $OldEnvFile }
    Ok "Using $name <- secrets.json > $name.App > DryCleaning\appsettings.secrets.json (git-ignored)"
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
    if (-not $mock -and -not (Test-Key $SecretsFile)) { Write-Host "  - No Google Maps key in $name.App > GoogleMaps > ApiKey (address search uses sample addresses)." }
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
    foreach ($f in @($OldEnvFile, $SecretsFile)) { if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f } }
    Ok 'Removed DryCleaning\appsettings.secrets.json - the app uses its built-in defaults (no environment, no keys).'
    Write-Host ''
    Show-Current
    exit 0
}
if (-not $Name) { Show-Usage; Write-Host ''; Show-List; exit 1 }
Use-Profile $Name $DeviceIp
