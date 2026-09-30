# START HERE — `ST.LiquorTNT` (Excise Track & Trace Merge Platform)

> Latest, final documentation set as of **2026-09-25**. Everything older (`TNT_LIQUOR_API`, `Excise.*` names, `dotnet-folder-structure-simple.md`, v1.1 standards, `project-overview-brief.md`) is superseded by this folder.
> Same set lives in three places: this project folder, `C:\Projects\Liquor_Application\docs\`, and (pointer only) `C:\Projects\Liquor_Application\CLAUDE.md`.

## Read in this order

| # | File | What it gives you | Time |
|---|---|---|---|
| 1 | `01-project-context.md` | What we are building and why, the 11 states, tenancy, scale, locked technology, final naming, status, open decisions | 10 min |
| 2 | `02-backend-architecture-standards.md` | **The rules.** v1.2. Layers, references, naming, API shape, business/domain/infrastructure rules, SP/view/trigger policy, dual provider, security, logging, testing, checklist, never-do list | 30 min |
| 3 | `03-solution-structure.md` | Every file in the solution today, what it does, where the next file goes, how to run | 10 min |
| 4 | `04-ai-coding-guide.md` | Condensed rules for an AI — paste before generating code | 5 min |
| 5 | `Backend-Architecture-Standards-Developer.docx` | Word copy of #2 for people who want a document | — |
| 6 | `05-user-module-api.md` | Plain-language guide to every User-module API: purpose, who may call it, request/response, errors | 15 min |

## The five sentences that matter most

1. **Layers:** `Api → Business → Domain`; `Infrastructure` implements interfaces that `Business` declares. `Domain` and `Contracts` reference nothing. `Desktop` references only `Contracts`. Architecture tests enforce this.
2. **Interfaces live in `Business`, in the folder of the feature that owns them.** Exactly two kinds: `I<Feature>Service`, and interfaces whose implementation lives in another project. Domain declares none.
3. **State differences are capabilities, never conditions.** `if (excise == "RJ")` outside `Business/Excise/*` is a defect.
4. **Tenant (`CompanyId`, `PlantId`, `ExciseCode`) comes only from the JWT** via `ITenantContext` — never from a request, and always passed explicitly to SPs/views.
5. **MySQL primary, SQL Server supported.** A feature is done when both providers are done; SQL objects exist in both `Scripts/<Provider>/` folders; LINQ composes over views, not over SPs.

## Where the code is

```
C:\Projects\Liquor_Application\ST.LiquorTNT.sln
  src\   Api · Business · Domain · Contracts · Infrastructure · Logging · Desktop · WebApplication(React)
  tests\ Domain.Tests · Business.Tests · Infrastructure.Tests · Api.Tests · Edge.Tests · Architecture.Tests
```

Login is implemented end to end and is the reference shape for every next module. Database: MySQL `st_tnt_liquor` on `192.168.1.99`; scripts in `db\mysql\`.

## Immediate next steps

1. `dotnet build ST.LiquorTNT.sln` and `dotnet test ST.LiquorTNT.sln` — both green.
2. Run `db\mysql\003`–`005` once (if not already), start `ST.LiquorTNT.Api`, `POST /api/auth/login` with `admin` / `Admin@123`.
3. The User module is complete (see `05-user-module-api.md`). Next: the Roles module with `[HasPermission]`, then Companies → Plants → Brands, following the module checklist in the standards §19.
