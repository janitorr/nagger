## 1. Dependency Bump

- [x] 1.1 Bump `Microsoft.EntityFrameworkCore.Sqlite` and `Microsoft.EntityFrameworkCore.Design` from `10.0.10` to `10.0.12` in `src/Nagger.Host/Nagger.Host.csproj` and verify no `10.0.10` EF Core reference remains in that file
- [x] 1.2 Bump `Microsoft.EntityFrameworkCore.Sqlite` from `10.0.10` to `10.0.12` in `tests/Nagger.Host.Tests/Nagger.Host.Tests.csproj` and verify no `10.0.10` EF Core reference remains in that file

## 2. Verification

- [x] 2.1 Run `dotnet restore` then `dotnet list package --vulnerable --include-transitive` and verify the output reports no vulnerable packages
- [x] 2.2 Run `dotnet build Nagger.slnx` and verify it completes without the `NU1903` warning
- [x] 2.3 Run `dotnet test Nagger.slnx` and verify all Core and Host tests pass on the updated dependency graph
- [x] 2.4 Run `dotnet run --project src/Nagger.Host` and verify the host starts, applies migrations, and serves requests against SQLite
