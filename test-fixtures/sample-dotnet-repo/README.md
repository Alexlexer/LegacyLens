# sample-dotnet-repo

Minimal .NET-style fixture used by the LegacyLens integration smoke test.

The fixture contains:

- `src/IUserService.cs` — interface
- `src/User.cs` — domain record
- `src/UserService.cs` — implementation (this file is modified by the prepare script)
- `src/IUserRepository.cs` — repository interface
- `src/UserRepository.cs` — in-memory repository (contains a clearly fake connection string)
- `src/UserController.cs` — controller calling the service
- `appsettings.json` — non-secret config only

**Connection strings in this fixture are intentionally fake and non-functional.**

## Usage

Run `prepare-sample-diff.ps1` (or `.sh`) from the repository root to initialise a local
Git repo here and leave a deterministic dirty change for LegacyLens to review.
See `docs/demo.md` for the full integration smoke-test workflow.
