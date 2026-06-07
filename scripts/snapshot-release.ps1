param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^v\d+\.\d+\.\d+$')]
    [string]$Version,

    [switch]$Push
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$branch = "snapshot/$Version"
$remote = "origin"

git diff --quiet
if ($LASTEXITCODE -ne 0) {
    throw "Working tree has unstaged changes. Commit or intentionally stage changes before snapshot."
}

git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    throw "Index has staged changes. Commit them before snapshot."
}

git rev-parse --verify $branch *> $null
if ($LASTEXITCODE -eq 0) {
    throw "Snapshot branch already exists: $branch"
}

git rev-parse --verify "refs/tags/$Version" *> $null
if ($LASTEXITCODE -eq 0) {
    throw "Tag already exists: $Version"
}

dotnet build .\TaskbarTransparency.csproj -c Release -p:Platform=x64
dotnet test .\tests\TaskbarTransparency.Tests\TaskbarTransparency.Tests.csproj -c Release

git switch -c $branch
try {
    git commit --allow-empty -m "snapshot: $Version"
    git tag -a $Version -m "snapshot: $Version"

    if ($Push) {
        git push $remote $branch
        git push $remote $Version
    }
}
catch {
    throw
}
