# Liquor Application - Version Register

Single place to see the version of every tool, framework, package and database script the
project depends on, split by application. Keep this file at the repository root next to
`.gitignore`, and update it in the same commit as any version change.

| | |
|---|---|
| Repository | `D:\TAPASVI\Project\Liquor Application` (git root) |
| Register version | 1.0.1 |
| Last updated | 2026-09-30 |
| Updated by | Dax Padaliya |

**How to read this file.** Section 1 is the .NET application (API, desktop line app, tests).
Section 2 is the Web Application (React). Section 3 is the shared database. Section 4 is the
history of this register. Anything marked `TBD` is not yet decided or not yet installed.

**Rule.** Versions are never typed directly into a `.csproj` or `package.json` dependency
without also updating this file. For .NET the source of truth is `Directory.Packages.props`;
for the web app it will be `package.json` + `package-lock.json`.

---

## 1. .NET Application

Folder: `.Net Application\`  
Solution: `ST.LiquorTNT.sln`  
Application version: **1.0.0** (default - no `<Version>` is set yet; see "Recommended next step" below)

### 1.1 Toolchain

| Item | Version | Where it is defined |
|---|---|---|
| .NET SDK / runtime | **.NET 8.0** (`net8.0`, LTS) | `Directory.Build.props` → `<TargetFramework>` |
| C# language version | `latest` (C# 12 with the .NET 8 SDK) | `Directory.Build.props` → `<LangVersion>` |
| Nullable reference types | enabled | `Directory.Build.props` |
| Implicit usings | enabled | `Directory.Build.props` |
| Warnings as errors | Release only | `Directory.Build.props` |
| Central package management | enabled | `Directory.Packages.props` → `ManagePackageVersionsCentrally=true` |
| Visual Studio (solution created with) | 17.10.34928.147 (VS 2022) | `ST.LiquorTNT.sln` header |
| Solution file format | 12.00 | `ST.LiquorTNT.sln` header |
| `global.json` | not present (any installed .NET 8 SDK is used) | - |

### 1.2 Projects

| Project | SDK | Target framework | Output | Notes |
|---|---|---|---|---|
| `src/ST.LiquorTNT.Api` | Microsoft.NET.Sdk.Web | net8.0 | Web API (ASP.NET Core 8) | Serves the React build from `wwwroot` in production |
| `src/ST.LiquorTNT.Business` | Microsoft.NET.Sdk | net8.0 | Class library | |
| `src/ST.LiquorTNT.Contracts` | Microsoft.NET.Sdk | net8.0 | Class library | No references at all - wire format shared with React and Desktop |
| `src/ST.LiquorTNT.Domain` | Microsoft.NET.Sdk | net8.0 | Class library | No references at all |
| `src/ST.LiquorTNT.Infrastructure` | Microsoft.NET.Sdk | net8.0 | Class library | EF Core + MySQL, JWT, hashing |
| `src/ST.LiquorTNT.Logging` | Microsoft.NET.Sdk | net8.0 | Class library | Serilog host logging |
| `src/ST.LiquorTNT.Desktop` | Microsoft.NET.Sdk | **net8.0-windows** | WinExe (WinForms) | Line application; Windows only |
| `tests/ST.LiquorTNT.Api.Tests` | Microsoft.NET.Sdk | net8.0 | xUnit | Uses `Microsoft.AspNetCore.App` framework reference |
| `tests/ST.LiquorTNT.Architecture.Tests` | Microsoft.NET.Sdk | net8.0 | xUnit | NetArchTest rules |
| `tests/ST.LiquorTNT.Business.Tests` | Microsoft.NET.Sdk | net8.0 | xUnit | |
| `tests/ST.LiquorTNT.Domain.Tests` | Microsoft.NET.Sdk | net8.0 | xUnit | |
| `tests/ST.LiquorTNT.Edge.Tests` | Microsoft.NET.Sdk | **net8.0-windows** | xUnit | Follows Desktop (WinForms) |
| `tests/ST.LiquorTNT.Infrastructure.Tests` | Microsoft.NET.Sdk | net8.0 | xUnit | Needs the MySQL dev database |

`src/ST.LiquorTNT.WebApplication` is a README placeholder only. It is **not** a .NET project
and is not in the solution; the real React app lives in `Web Application\` (Section 2).

### 1.3 NuGet packages

All versions come from `Directory.Packages.props`. Individual `.csproj` files only list the
package name.

**Data**

| Package | Version | Used by |
|---|---|---|
| Microsoft.EntityFrameworkCore | 8.0.10 | Infrastructure |
| Microsoft.EntityFrameworkCore.Relational | 8.0.10 | Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 8.0.10 | Infrastructure (PrivateAssets=all) |
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | Infrastructure |

**Authentication**

| Package | Version | Used by |
|---|---|---|
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.10 | Api |
| System.IdentityModel.Tokens.Jwt | 7.6.2 | Infrastructure |

**Microsoft.Extensions**

| Package | Version | Used by |
|---|---|---|
| Microsoft.Extensions.Options | 8.0.2 | Business |
| Microsoft.Extensions.Options.ConfigurationExtensions | 8.0.0 | Infrastructure |
| Microsoft.Extensions.Configuration.Abstractions | 8.0.0 | Infrastructure |
| Microsoft.Extensions.Hosting.Abstractions | 8.0.1 | (declared, not referenced by any project yet) |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | (declared, not referenced by any project yet) |

**Validation / API docs**

| Package | Version | Used by |
|---|---|---|
| FluentValidation | 11.9.2 | Business |
| FluentValidation.DependencyInjectionExtensions | 11.9.2 | Api, Business |
| Swashbuckle.AspNetCore | 6.6.2 | Api |

**Logging**

| Package | Version | Used by |
|---|---|---|
| Serilog.AspNetCore | 8.0.2 | Logging |
| Serilog.Sinks.File | 6.0.0 | Logging |

**Testing**

| Package | Version | Used by |
|---|---|---|
| Microsoft.NET.Test.Sdk | 17.11.1 | all test projects |
| xunit | 2.9.0 | all test projects |
| xunit.runner.visualstudio | 2.8.2 | all test projects |
| FluentAssertions | 6.12.1 | all test projects |
| NetArchTest.Rules | 1.3.2 | Architecture.Tests |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.10 | Api.Tests |

### 1.4 Runtime configuration that carries a version

| Setting | Value | File |
|---|---|---|
| `Database:Provider` | MySql | `src/ST.LiquorTNT.Api/appsettings.json` |
| `Database:ServerVersion` | 8.0.36 | `src/ST.LiquorTNT.Api/appsettings.json` |
| `Cors:AllowedOrigins` | `http://localhost:5173`, `http://localhost:3000` | `src/ST.LiquorTNT.Api/appsettings.json` |
| Log files | `logs/stliquortnt-YYYYMMDD.json`, daily roll, 100 MB, keep 30 | `src/ST.LiquorTNT.Api/appsettings.json` |

### 1.5 How to update .NET versions

1. Change the number in `Directory.Packages.props` (never in a `.csproj`).
2. Run `dotnet restore ST.LiquorTNT.sln` and `dotnet test ST.LiquorTNT.sln`.
3. Update the matching row in this file and add a line to Section 4.

To move the whole solution to a newer .NET, change `<TargetFramework>` in
`Directory.Build.props` and the two `net8.0-windows` overrides (Desktop, Edge.Tests).

**Recommended next step.** Add an application version in one place so every DLL and the
`API starting` log entry carry it:

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <Version>1.0.0</Version>
</PropertyGroup>
```

---

## 2. Web Application (React)

Folder: `Web Application\`  
Status: **scaffolded and running** - this section is still the pre-scaffold plan and is OUT OF DATE; regenerate it from `Web Application\package.json` and `package-lock.json`.  
Application version: **0.0.0** (to be set in `package.json` → `"version"` when the app is created)

### 2.1 Planned toolchain (from `ST.LiquorTNT.WebApplication/README.md` and the frontend standards)

| Item | Planned | Version | Where it will be defined |
|---|---|---|---|
| Node.js | required | TBD - record the LTS you install (`node -v`) | `.nvmrc` / `package.json` → `"engines"` |
| npm | required | TBD (`npm -v`) | `package-lock.json` |
| Scaffold command | `npm create vite@latest . -- --template react-ts` | - | - |
| Vite | build tool | TBD | `package.json` → `devDependencies` |
| React / React DOM | UI | TBD | `package.json` → `dependencies` |
| TypeScript | language | TBD | `package.json` → `devDependencies` |
| MUI (`@mui/material`) | component library | TBD | `package.json` |
| TanStack Query (`@tanstack/react-query`) | server state | TBD | `package.json` |
| react-hook-form | forms | TBD | `package.json` |
| zod | validation | TBD | `package.json` |
| OpenAPI type generator | API types from Swagger | TBD (tool not chosen yet) | `package.json` |
| Dev server | `http://localhost:5173` | - | already allowed in API CORS |
| Production output | `dist/` → copied to `ST.LiquorTNT.Api/wwwroot` | - | - |

### 2.2 Dependencies (fill in after scaffolding)

Once the app exists, run this in `Web Application\` and paste the result into the tables below:

```
npm ls --depth=0
```

**dependencies**

| Package | Version |
|---|---|
| _(none yet)_ | |

**devDependencies**

| Package | Version |
|---|---|
| _(none yet)_ | |

### 2.3 How to update web versions

1. Change the version with `npm install <package>@<version>` so `package-lock.json` stays in sync.
2. Run `npm run build` and `npm run lint`.
3. Update the matching row in this file and add a line to Section 4.

Never edit `package-lock.json` by hand, and never commit `node_modules/` (already ignored by the
root `.gitignore`).

---

## 3. Database

Shared by both applications through the API. The React app and the Desktop app never open a
database connection.

| Item | Value |
|---|---|
| Engine | MySQL |
| Server version the API is configured for | 8.0.36 (`Database:ServerVersion`) |
| Dev server | `192.168.1.99:3306`, database `st_tnt_liquor` |
| Secondary supported provider | SQL Server (wiring planned, not yet in the solution) |

**Schema scripts** (`.Net Application\db\mysql\`, run once, in order)

| Script | Purpose |
|---|---|
| `003_user_module_schema.sql` | User-module tables + seeds |
| `004_users_remove_excise_plant.sql` | drops `EXCISE_CODE` / `ALLOTED_PLANT_ID` from `USERS` |
| `005_user_module_phase2.sql` | `USER_LOG` nullable columns, reset-token column, admin credential |
| `006_log_mode.sql` | `SECURITY_CONFIG.LOG_MODE` |
| `007_session_sliding.sql` | `USER_SESSION.ABSOLUTE_EXPIRES_AT`, `SESSION_IDLE_MINUTES` |
| `008_session_full_behaviour.sql` | `SECURITY_CONFIG.SESSION_FULL_BEHAVIOUR` accepts `REJECT_ALLOW_EVICT` |

Current schema level: **008**. The next script must be `009_*.sql`; add it to this table when
you create it.

---

## 4. Change history of this register

| Date | Register version | Application | Change | By |
|---|---|---|---|---|
| 2026-09-30 | 1.0.0 | all | First version. Captured .NET 8 solution, all NuGet versions from `Directory.Packages.props`, MySQL 8.0.36, schema scripts 003-007. Web Application not yet scaffolded. | Dax Padaliya |
| 2026-09-30 | 1.0.1 | Database | Added schema script `008_session_full_behaviour.sql`; schema level is now 008. Section 2 still describes the pre-scaffold plan and needs regenerating from the shipped `package.json` / `package-lock.json`. | Dax Padaliya |
