# AquaFarm Pro Release Readiness Plan

## Current Status

The project is not ready for distribution yet. The first release gate is a clean build from source. The current environment cannot verify that gate because the .NET SDK is not available on PATH.

Run the release gate with:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\verify-release-readiness.ps1
```

Publish after build and tests pass:

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\verify-release-readiness.ps1 -Publish
```

## Phase 1: Establish The Baseline

- Install .NET 8 SDK on the build machine.
- Run the release readiness script.
- Fix all `dotnet build -c Release` errors.
- Do not treat existing `bin` or `obj` output as proof of readiness.

## Phase 2: Clean Distribution Structure

- Publish output goes to `Setup\publish`.
- Installer output goes to `artifacts\installer`.
- Do not include local databases, build logs, or developer artifacts in the installer.
- Fix Arabic documentation encoding before customer delivery.

## Phase 3: Production Safety

- Demo mode is controlled by `AppSettings:SeedDemoData`.
- The default config now uses `SeedDemoData=false`.
- If no users exist, `FirstRunAdminForm` creates the first administrator account.
- Default demo credentials must not be logged.
- Password reset now generates a strong temporary password instead of using a fixed value.

## Phase 4: Tests

- Convert `Tests\DatabaseIntegrityTest.cs` into real xUnit tests using `[Fact]`.
- Cover login, customer creation, sales orders, tax invoices, migrations, and backup.
- Test failure must block release.

## Phase 5: Publish And Installer

- Run `verify-release-readiness.ps1 -Publish`.
- Build `Setup\FishFarmManagerSetup.iss`.
- Test install, launch, uninstall, and reinstall on a clean Windows profile.

## Phase 6: Final Release Review

- Inspect installer contents.
- Update version and changelog.
- Document known limitations.
- Ship a single named artifact, for example `AquaFarmPro-1.0.0-win-x64-setup.exe`.
