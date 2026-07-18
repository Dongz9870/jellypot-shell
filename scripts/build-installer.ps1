[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(\.\d+)?$')]
    [string]$Version = '0.8.0',

    [string]$InnoCompilerPath = '',

    [switch]$KeepExistingWebView2Bootstrapper
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = [IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$artifactRoot = [IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'artifacts'))
$publishDirectory = Join-Path $artifactRoot 'publish\win-x64'
$prerequisiteDirectory = Join-Path $artifactRoot 'prerequisites'
$installerOutputDirectory = Join-Path $artifactRoot 'installer'
$projectPath = Join-Path $repositoryRoot `
    'src\JellyfinPotPlayerShell.App\JellyfinPotPlayerShell.App.csproj'
$installerScript = Join-Path $repositoryRoot `
    'installer\JellyfinPotPlayerShell.iss'
$webView2Bootstrapper = Join-Path $prerequisiteDirectory `
    'MicrosoftEdgeWebview2Setup.exe'

function Reset-ArtifactDirectory([string]$Path) {
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $allowedRoot = $artifactRoot.TrimEnd('\') + '\'
    if (-not $resolvedPath.StartsWith(
            $allowedRoot,
            [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a path outside artifacts: $resolvedPath"
    }

    if (Test-Path -LiteralPath $resolvedPath) {
        Remove-Item -LiteralPath $resolvedPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $resolvedPath -Force | Out-Null
}

function Resolve-InnoCompiler([string]$ConfiguredPath) {
    if ($ConfiguredPath) {
        $resolved = [IO.Path]::GetFullPath($ConfiguredPath)
        if (Test-Path -LiteralPath $resolved -PathType Leaf) {
            return $resolved
        }

        throw "The configured Inno Setup compiler was not found: $resolved"
    }

    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    throw 'Inno Setup 6 was not found. Install it and run this script again.'
}

Reset-ArtifactDirectory $publishDirectory
Reset-ArtifactDirectory $installerOutputDirectory
New-Item -ItemType Directory -Path $prerequisiteDirectory -Force | Out-Null

if (-not $KeepExistingWebView2Bootstrapper -or
    -not (Test-Path -LiteralPath $webView2Bootstrapper -PathType Leaf)) {
    Invoke-WebRequest `
        -Uri 'https://go.microsoft.com/fwlink/p/?LinkId=2124703' `
        -OutFile $webView2Bootstrapper `
        -UseBasicParsing
}

$signature = Get-AuthenticodeSignature -LiteralPath $webView2Bootstrapper
if ($signature.Status -ne 'Valid' -or
    $signature.SignerCertificate.Subject -notmatch 'Microsoft') {
    throw 'The WebView2 Bootstrapper Microsoft signature is invalid.'
}

dotnet publish $projectPath `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:Version=$Version `
    -o $publishDirectory
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$requiredPublishFiles = @(
    'JellyfinPotPlayerShell.exe',
    'JellyfinPotPlayerShell.dll',
    'Microsoft.Web.WebView2.Core.dll',
    'Microsoft.Web.WebView2.Wpf.dll',
    'runtimes\win-x64\native\WebView2Loader.dll',
    'Assets\inject.js',
    'Assets\jellyfin-web-adapter.js',
    'Assets\potplayer-button.css',
    'appsettings.json'
)
foreach ($relativePath in $requiredPublishFiles) {
    $fullPath = Join-Path $publishDirectory $relativePath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "The publish directory is missing: $relativePath"
    }
}

$compiler = Resolve-InnoCompiler $InnoCompilerPath
& $compiler `
    "/DAppVersion=$Version" `
    "/DPublishDir=$publishDirectory" `
    "/DPrerequisiteDir=$prerequisiteDirectory" `
    "/DInstallerOutputDir=$installerOutputDirectory" `
    $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $installerOutputDirectory `
    'JellyfinPotPlayerShell-Setup-x64.exe'
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "The expected installer was not created: $installerPath"
}

Write-Output "Installer created: $installerPath"
