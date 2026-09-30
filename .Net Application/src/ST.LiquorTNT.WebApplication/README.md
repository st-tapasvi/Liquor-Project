# ST.LiquorTNT.WebApplication

The React front end (Vite + TypeScript). **Not a .NET project** - it is not part of `ST.LiquorTNT.sln`.

Planned setup:

```
npm create vite@latest . -- --template react-ts
```

Structure and rules follow `frontend-architecture-standards.md`:
`app / features / entities / shared / core`, TanStack Query for server state, MUI, react-hook-form + zod,
and types generated from the API's OpenAPI document.

The production build is copied into `ST.LiquorTNT.Api/wwwroot` so one installation serves both.

During development the app runs on `http://localhost:5173` and calls the API directly - that origin is
already allowed in the API's CORS settings.
