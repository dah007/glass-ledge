<#
.SYNOPSIS
    Builds the Microsoft Store (MSIX) package for GlassLedge.

.DESCRIPTION
    1. Publishes the app self-contained for win-x64 (artifacts\publish-msix\win-x64).
    2. Stages it with the manifest (identity filled in from packaging\msix\store-identity.json) and the package logos.
    3. Builds resources.pri with makepri and the package with makeappx.
    4. With -TestSign (or while the identity is still a placeholder), also makes a self-signed copy you can install
       on this PC to try the package.

    The file to upload to Partner Center is the UNSIGNED GlassLedge-<version>.0-x64.msix: the Store signs it for you.
    A package built with the placeholder identity (-DEV in its name) must never be uploaded.

    Needs the Windows 10/11 SDK (makeappx.exe, makepri.exe, signtool.exe) and the .NET SDK.

.PARAMETER Version
    Version to stamp on the app and package, like 1.2.3. Defaults to <Version> in Directory.Build.props.
    The package version is Version.0: the Store reserves the fourth part, so it must stay 0.

.PARAMETER TestSign
    Also produce a self-signed package for installing locally.

.EXAMPLE
    .\build\build-msix.ps1 -TestSign
#>
[CmdletBinding()]
param(
    [string] $Version,
    [switch] $TestSign
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$runtime = 'win-x64'

function Find-SdkTool([string] $name) {
    $onPath = Get-Command $name -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }
    $kits = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
    $found = Get-ChildItem $kits -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' } |
        Sort-Object { [version] $_.Name } -Descending |
        ForEach-Object { Join-Path $_.FullName "x64\$name" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if (-not $found) { throw "$name not found. Install the Windows 10/11 SDK (it includes makeappx, makepri and signtool)." }
    return $found
}

function Assert-Success([string] $step) {
    if ($LASTEXITCODE -ne 0) { throw "$step failed (exit code $LASTEXITCODE)." }
}

# --- version ---------------------------------------------------------------
if (-not $Version) {
    [xml] $props = Get-Content (Join-Path $root 'Directory.Build.props')
    $Version = ($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$Version' must look like 1.2.3." }
$packageVersion = "$Version.0"
Write-Host "Package version $packageVersion" -ForegroundColor Green

# --- identity --------------------------------------------------------------
$identity = Get-Content (Join-Path $root 'packaging\msix\store-identity.json') -Raw | ConvertFrom-Json
$forStore = ($identity.identityName -notmatch 'REPLACE') -and
            ($identity.publisher -notmatch 'REPLACE') -and
            ($identity.publisherDisplayName -notmatch 'REPLACE')
if ($forStore) {
    $identityName = $identity.identityName
    $publisher = $identity.publisher
    $publisherDisplayName = $identity.publisherDisplayName
    $suffix = ''
} else {
    Write-Warning 'store-identity.json still has placeholders: building a local test package (-DEV). Do not upload it to the Store.'
    $identityName = 'GlassLedge.Dev'
    $publisher = 'CN=GlassLedge Dev'
    $publisherDisplayName = 'GlassLedge Dev'
    $suffix = '-DEV'
    $TestSign = $true   # an unsigned package can't be installed, so a dev build is always test-signed
}

$makeappx = Find-SdkTool 'makeappx.exe'
$makepri = Find-SdkTool 'makepri.exe'
if ($TestSign) { $signtool = Find-SdkTool 'signtool.exe' }

# --- publish ---------------------------------------------------------------
$publishDir = Join-Path $root "artifacts\publish-msix\$runtime"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
Write-Host 'Publishing...' -ForegroundColor Cyan
dotnet publish (Join-Path $root 'src\GlassLedge.App\GlassLedge.App.csproj') `
    -c Release -r $runtime --self-contained true "-p:Version=$Version" -o $publishDir
Assert-Success 'dotnet publish'

# --- stage -----------------------------------------------------------------
$work = Join-Path $root 'artifacts\msix'
$stage = Join-Path $work 'stage'
$priRoot = Join-Path $work 'pri'
foreach ($dir in $stage, $priRoot) { if (Test-Path $dir) { Remove-Item $dir -Recurse -Force } }
New-Item -ItemType Directory -Path $stage, $priRoot | Out-Null

Copy-Item (Join-Path $publishDir '*') $stage -Recurse
Get-ChildItem $stage -Filter *.pdb -Recurse | Remove-Item -Force
Copy-Item (Join-Path $root 'packaging\msix\Assets') (Join-Path $stage 'Assets') -Recurse
Copy-Item (Join-Path $root 'packaging\msix\Assets') (Join-Path $priRoot 'Assets') -Recurse

function Escape-Xml([string] $text) { [System.Security.SecurityElement]::Escape($text) }
$manifest = Get-Content (Join-Path $root 'packaging\msix\AppxManifest.template.xml') -Raw
$manifest = $manifest.Replace('{{IdentityName}}', (Escape-Xml $identityName)).
    Replace('{{Publisher}}', (Escape-Xml $publisher)).
    Replace('{{PublisherDisplayName}}', (Escape-Xml $publisherDisplayName)).
    Replace('{{Version}}', $packageVersion)
if ($manifest -match '\{\{') { throw 'The manifest still has an unfilled {{token}}.' }
$manifestPath = Join-Path $stage 'AppxManifest.xml'
[System.IO.File]::WriteAllText($manifestPath, $manifest, (New-Object System.Text.UTF8Encoding($false)))

# --- resources.pri (lets Windows pick the right logo size) -----------------
Write-Host 'Indexing logos...' -ForegroundColor Cyan
$priConfig = Join-Path $work 'priconfig.xml'
& $makepri createconfig /cf $priConfig /dq en-US /pv 10.0.0 /o
Assert-Success 'makepri createconfig'
& $makepri new /pr $priRoot /cf $priConfig /mn $manifestPath /of (Join-Path $stage 'resources.pri') /o
Assert-Success 'makepri new'

# --- pack ------------------------------------------------------------------
$package = Join-Path $work "GlassLedge-$packageVersion-x64$suffix.msix"
if (Test-Path $package) { Remove-Item $package -Force }
Write-Host 'Packing...' -ForegroundColor Cyan
& $makeappx pack /d $stage /p $package /o
Assert-Success 'makeappx pack'
Write-Host "Built $package" -ForegroundColor Green

# --- optional test signing -------------------------------------------------
if ($TestSign) {
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $publisher } | Select-Object -First 1
    if (-not $cert) {
        $cert = New-SelfSignedCertificate -Type Custom -Subject $publisher -KeyUsage DigitalSignature `
            -FriendlyName 'GlassLedge test certificate' -CertStoreLocation 'Cert:\CurrentUser\My' `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    }
    $cer = Join-Path $work 'GlassLedge-test.cer'
    Export-Certificate -Cert $cert -FilePath $cer | Out-Null

    if ($suffix) {
        $signed = $package
    } else {
        $signed = Join-Path $work "GlassLedge-$packageVersion-x64-test-signed.msix"
        Copy-Item $package $signed -Force
    }
    & $signtool sign /fd SHA256 /sha1 $cert.Thumbprint $signed
    Assert-Success 'signtool sign'

    Write-Host ''
    Write-Host "Test package: $signed" -ForegroundColor Green
    Write-Host 'To install it on this PC (once, in an elevated PowerShell):' -ForegroundColor Yellow
    Write-Host "  Import-Certificate -FilePath '$cer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople"
    Write-Host "  Add-AppxPackage '$signed'"
    Write-Host 'To remove it: Get-AppxPackage *GlassLedge* | Remove-AppxPackage'
}

if ($forStore) {
    Write-Host ''
    Write-Host "Upload this to Partner Center (unsigned; the Store signs it): $package" -ForegroundColor Green
}
