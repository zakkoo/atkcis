# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ATK CIS (Check-In System) is a .NET 10 application for managing attendee check-ins at events. It consists of three projects sharing a common service library and SQLite database.

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run all tests
dotnet test

# Run a single test class or method
dotnet test --filter "FullyQualifiedName~CheckInDeskServiceTests"

# Run the web app
dotnet run --project Atk.Cis.Web

# Run the worker
dotnet run --project Atk.Cis.Worker

# Build CSS (from Atk.Cis.Web directory)
npm run build
```

## Architecture

The solution has three projects plus a shared library:

- **`Atk.Cis.Service`** — Shared business logic library used by both Web and Worker. Contains `CheckInDeskService`, EF Core `AppDbContext`, and all models/DTOs. Both other projects register this via `ServiceCollectionExtensions.AddCheckInDeskService()`.
- **`Atk.Cis.Web`** — ASP.NET Core Razor Pages frontend. Provides manual check-in/check-out UI, sign-up, barcode retrieval, and an admin panel.
- **`Atk.Cis.Worker`** — Background hosted service that polls console input for barcode scans. Automatically toggles check-in/check-out, plays audio feedback (`NetCoreAudio`), and periodically cleans up stale sessions.
- **`Atk.Cis.Service.Test`** — xUnit tests for the service layer using an in-memory SQLite database.

Both Web and Worker share the same SQLite database (default: `~/atkcis.db`).

## Key Concepts

**Session model**: A `UserSession` is opened on check-in and closed on check-out. `ClosedBy` tracks whether it was closed by the user (Web) or the worker (barcode scan). Stale sessions older than `MaxDurationMinutes` are cleaned up by the worker.

**Barcode generation**: User barcodes are Code128 encoded and derived from normalized initials (last name first 2 chars + first name first char). Collision handling appends incrementing digits.

**Admin password**: Configured in `appsettings.json` under `Admin:Password` (default: `atkadmin`). Admin pages are under `Pages/Admin/`.

## Configuration

Both `appsettings.json` files share the same keys:

| Key | Default | Description |
|-----|---------|-------------|
| `Database:Path` | `~/atkcis.db` | SQLite file path (supports `~` and env vars) |
| `SessionCleanup:MaxDurationMinutes` | `480` | Sessions older than this are stale |
| `SessionCleanup:WorkerIntervalMinutes` | `240` | How often Worker runs cleanup |
| `AudioOn` | `true` | (Worker only) Enable audio feedback |
| `Admin:Password` | `atkadmin` | (Web only) Admin panel password |

## CI/CD

GitHub Actions (`.github/workflows/build.yml`) triggers on `v*` tags. It runs tests, then publishes self-contained binaries for `linux-x64` and `linux-arm64`, and creates a GitHub release with zipped artifacts.

## Testing Guidelines

### Project & Namespace
- All tests live in `Atk.Cis.Service.Test/`
- Use namespace `Atk.Cis.Service.Tests` (not `Atk.Cis.Worker.Tests`)
- File naming: `[ClassName]Tests.cs`

### Test Naming
Follow the pattern: `[Method]_[Scenario]_[ExpectedResult]`

Examples:
- `CheckIn_ReturnsError_WhenCodeNotFound`
- `SignUp_AppendsDigit_OnCodeCollision`

### Database Setup
Each test creates an isolated in-memory EF Core context — **never share context between tests**:

```csharp
private static AppDbContext CreateContext()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
        .Options;
    return new AppDbContext(options);
}
```

Use `await using` to dispose after each test. No `IClassFixture` for database state.

### AAA Structure
Every test must have distinct Arrange / Act / Assert blocks (via comments or blank lines).

### xUnit Patterns
- `[Fact]` for single-scenario tests
- `[Theory]` + `[InlineData]` for multiple inputs (e.g., code normalization variants, edge-case strings)
- No `[SetUp]`/`[TearDown]` — keep tests self-contained

### What to Test
- **Happy path** for every public method on `ICheckInDeskService`
- **Error paths**: invalid/null codes, user not found, already checked in/out, duplicate sign-up
- **Edge cases for `SignUp`**: code collision → digit appended; diacritics stripped (`é` → `e`)
- Return value assertions AND database state assertions

### Coverage Priority (for new tests)
1. `CheckIn` / `CheckOut` error cases (invalid code, already-in-state)
2. `SignUp` (happy path + collision + diacritic normalization)
3. `IsCheckedIn` (true/false/invalid)
4. `GetUsers` / `GetUserSessions` (data returned correctly)
5. `GetBarcode` (returns non-empty SVG string)

### What NOT to Do
- Do not use actual SQLite or touch `~/atkcis.db` in tests
- Do not share `AppDbContext` instances across tests
- Do not test EF Core itself (e.g., don't assert `SaveChanges` was called)
- Do not create placeholder test files with no content
