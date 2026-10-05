# Build and deployment

## Build

```bash
npm ci
npm run build        # typecheck + vite build → dist/
```

`dist/` contains `index.html`, the content-hashed `assets/*.js` and `*.css`, `favicon.svg` and `robots.txt`. Source maps (`*.map`) are generated with `sourcemap: 'hidden'`: the bundle does not reference them, so browsers never request them. Keep them for symbolising client log stack traces, but **do not copy `*.map` files to the web root**.

Environment is baked in at build time from `.env.production` (`VITE_API_BASE_URL=/api`, mocks off). There are no runtime secrets; a different API address needs a different build, or a reverse-proxy rule, never an edit to the built files.

## One installation, one origin

The API serves the React app at the root URL and the JSON endpoints under `/api`, so the customer installs one Windows service (or IIS site) and the browser sees one origin. The publish step copies `dist/` into `ST.LiquorTNT.Api/wwwroot/`:

```powershell
# from the repository root, after `dotnet publish` of the API
Remove-Item -Recurse -Force "src\ST.LiquorTNT.Api\wwwroot\*" -ErrorAction Ignore
Copy-Item -Recurse "src\ST.LiquorTNT.WebApplication\dist\*" "src\ST.LiquorTNT.Api\wwwroot\"
Get-ChildItem "src\ST.LiquorTNT.Api\wwwroot" -Recurse -Include *.map | Remove-Item
```

The API side (static files with cache headers, the SPA fallback that serves `index.html` for any non-`/api` path so deep links reload, security headers and CSP) is described in [backend-changes.md](backend-changes.md).

## Caching

Hashed assets under `/assets/` are immutable: `Cache-Control: public, max-age=31536000, immutable`. `index.html` must be `Cache-Control: no-store` so a new release is picked up on the next load. With these two rules an upgrade is: publish the new files, done; users get the new version on their next navigation without clearing anything.

## TLS and headers

Production runs over HTTPS only — the `jwt` cookie is `Secure` and the API refuses to issue it otherwise. Terminate TLS at Kestrel with the customer's certificate, or at a reverse proxy (IIS ARR, nginx) in front of it; in the proxy case forward `X-Forwarded-Proto` and enable `UseForwardedHeaders` in the API so it knows the request was HTTPS. HSTS, CSP and the other headers come from the API middleware. When a proxy sits in front, do not let it add a second, weaker CSP.

## Environments

|                | API base                                  | Mocks                 | Log level | Notes                                           |
| -------------- | ----------------------------------------- | --------------------- | --------- | ----------------------------------------------- |
| Development    | `/api` → proxy to `http://localhost:5180` | optional              | debug     | `.env.development.local` for personal overrides |
| Test / staging | `/api`                                    | never                 | info      | Playwright runs against it with `E2E_BASE_URL`  |
| Production     | `/api`                                    | never (build refuses) | info      | one origin with the API                         |

## Release checklist

Run `npm run check` (typecheck, lint, format, tests, audit) and `npm run build`; tag the version in `package.json` (`npm version minor`) — the version is shown on the login page and in log entries; update `VERSIONS.md` at the repository root; publish the API with the new `wwwroot`; open the login page and confirm the version in the footer; check the API log for the `API starting` entry and one successful login.

## Rollback

Because the app is static files inside the API's `wwwroot`, rolling back is redeploying the previous API publish folder. Sessions are server-side, so a rollback does not log anyone out.
