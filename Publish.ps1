# Panoramic Data NuGet Publish Script (Standard)
$ErrorActionPreference = 'Stop'

$status = git status --porcelain
if ($status) {
	Write-Error "Working tree is not clean. Commit or stash changes before publishing.`n$status"
	exit 1
}

$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne 'main') {
	Write-Error "Publishing is only supported from the 'main' branch (currently on '$branch')."
	exit 1
}

git fetch origin main --quiet
$localHead = git rev-parse HEAD
$remoteHead = git rev-parse origin/main
if ($localHead -ne $remoteHead) {
	Write-Error 'Local branch is not up to date with origin/main. Pull or push first.'
	exit 1
}

$project = Join-Path $PSScriptRoot 'PanoramicData.LicenceMagic/PanoramicData.LicenceMagic.csproj'
$buildOutput = dotnet build $project -t:GetBuildVersion --getProperty:NuGetPackageVersion -nologo -v:quiet -p:TreatWarningsAsErrors=false
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to determine version from Nerdbank.GitVersioning.`n$buildOutput"
	exit 1
}
$version = ($buildOutput | Select-Object -Last 1).ToString().Trim()
if (-not $version) {
	Write-Error 'Failed to determine version from Nerdbank.GitVersioning.'
	exit 1
}
if (git tag -l $version) {
	Write-Error "Tag '$version' already exists."
	exit 1
}

Write-Host "Tagging as $version ..." -ForegroundColor Cyan
git tag $version
git push origin $version
Write-Host "Published tag $version; CI will build and push to NuGet." -ForegroundColor Green
