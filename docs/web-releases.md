# OrderHub Web releases

The only release workflow is [.github/workflows/web-release.yml](../.github/workflows/web-release.yml). It runs on pushed version tags, never ordinary branch pushes or pull requests. Accepted production versions are canonical `vX.Y.Z` tags, such as `v1.0.0`; prerelease suffixes and leading zeros are rejected.

## Release gates

The tag's commit, the event commit, and the latest remote `master` commit must match. Both lightweight and annotated tags resolve to their underlying commit. The workflow fetches `master` and the tag again before draft creation and immediately before publication. Do not advance `master` or move the tag while a release is running.

The build job has only `contents: read`. The separate release job has `contents: write`, which is required to create releases using the built-in `GITHUB_TOKEN`. No personal token or repository secret is needed. Release runs for the same tag are serialized; existing releases and drafts block creation and are never overwritten.

Release assets are uploaded to a draft first. Publication happens only after the build job succeeds, the checksum and two uploaded asset names are verified, and the production commit is rechecked. An interrupted upload or failed final check may leave a draft, but no published release. Review and explicitly delete an incomplete draft in GitHub before rerunning failed jobs. Never delete or overwrite a completed production release to make a rerun succeed.

## Build and tests

`global.json` selects a stable .NET 9 SDK; Actions installs the latest `9.0.x` SDK. The workflow restores dependencies, builds the complete solution in Release, runs the .NET test suite, and runs the Node.js browser-local time test.

PostgreSQL and release-rehearsal tests are intentionally not enabled. Their opt-in connection settings are absent, so they report skips. Several of these tests apply migrations or read a configured source database, which this release workflow must not do. The remaining tests use mocks or isolated in-memory SQLite databases; they do not access production. No application startup, migration command, installer, or deployment step is run.

## Release files and production configuration

The workflow publishes `OrderHub.Web/OrderHub.Web.csproj` for `win-x64`, self-contained, with `Version` extracted from the tag. The two assets are:

- `OrderHub-Web-vX.Y.Z-win-x64.zip`
- `OrderHub-Web-vX.Y.Z-win-x64.zip.sha256` (SHA256 followed by the ZIP filename)

The ZIP contains the application, bundled .NET/ASP.NET Core runtimes, required dependencies, Web assets, and PDF images. Source files, test outputs, debug symbols, browser source maps, and environment-specific `appsettings.*.json` files are excluded. Only the credential-free `appsettings.json` base settings remain. Package validation rejects unexpected files or deployment-specific base configuration.

Windows Service hosting remains `OrderHubWeb`. Production configuration must already be provisioned outside the archive at `%ProgramData%/OrderHub/Config/appsettings.Server.json`, with the connection string, data paths, bindings, and service-account permissions appropriate for that server. This workflow only creates downloadable release assets; it neither configures nor deploys the service. Database changes require a separate, explicitly authorized operation following the [migration runbook](order-lifecycle-release-runbook.md).

## First production release

1. Review the workflow and enable GitHub Actions for the repository. Repository/organization policy must allow the official `actions/*` actions used here and `contents: write` in the release job. No custom secrets are required.
2. Commit these changes to `master` and push it. Ordinary pushes do not release anything.
3. Choose an unused production version. The example below uses `v1.0.0`; replace it if that tag or release already exists.
4. Tag the latest remote `master` commit and push only that tag. Keep `master` unchanged until the release finishes.

Run from the repository root in PowerShell:

```powershell
git switch master
git pull --ff-only origin master
git add .github/workflows/web-release.yml global.json OrderHub.Web/OrderHub.Web.csproj README.md docs/web-releases.md
git commit -m "Configure OrderHub Web release workflow"
git push origin master
git fetch origin master
if ((git rev-parse HEAD) -ne (git rev-parse origin/master)) {
    throw 'Local HEAD is not the latest remote master commit.'
}
git tag -a v1.0.0 -m "OrderHub Web v1.0.0"
git push origin refs/tags/v1.0.0
```

5. Watch **Actions → OrderHub Web release**. On success, **Releases** contains the published tag, automatically generated notes, ZIP, and checksum.
6. Verify a downloaded ZIP before using it:

```powershell
$zip = 'OrderHub-Web-v1.0.0-win-x64.zip'
$expected = ((Get-Content "$zip.sha256" -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expected) {
    throw 'Checksum mismatch.'
}
```

The workflow has been validated locally, but creating/uploading/publishing a real GitHub Release can only be verified on an authorized tag push. Local package verification uses a sample version without creating a Git tag or GitHub Release.
