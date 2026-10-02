# Security

The application is part of a liquor excise Track & Trace system, so it is built secure by default and in depth: every control below is one layer, and no single layer is trusted on its own. No system can be guaranteed unbreakable; the aim is to follow strong industry practice and to keep shrinking the attack surface release after release.

## Threat model in one paragraph

The users are plant staff and excise administrators on an on-premise network, occasionally over the internet through a reverse proxy. The assets are user accounts, security settings and, later, production and dispatch data that regulators rely on. The attacker is anyone who can reach the web server, run script in a user's browser (XSS through a third-party or a future bug), sit on the network path, or publish a malicious package into the dependency tree.

## Authentication: JWT in an HttpOnly cookie, session row on the server

The browser's JavaScript never holds a credential. Login creates a session on the server (`USER_SESSION`) and issues a JWT whose hash the row stores; the JWT is delivered only in a cookie named `jwt` with `HttpOnly; Secure; SameSite=Lax; Path=/`. The login response body carries no token. There is nothing in `localStorage`, `sessionStorage`, memory, or the URL for script to steal; a stolen page cannot exfiltrate the token, only use it while the page is open. The API validates the JWT on every call, then checks the session row behind it: idle timeout, 24-hour absolute expiry, logout and revocation all take effect immediately, and last activity is updated. Logout revokes the row and clears the cookie. `GET /api/auth/me` at start-up is the only way the app learns who is logged in, so a revoked session can never be "remembered" client-side.

Three session-ending outcomes are handled in one place (`error.interceptor.ts`): `SESSION_TIMED_OUT` and `SESSION_INVALID` clear state and show the login page with the reason; `SESSION_EXPIRED` (the hard limit) parks the failed request, asks for the password in a dialog, opens a new session and retries the parked requests, so nothing on screen is lost.

### The device limit and "sign out this device"

`SECURITY_CONFIG.MAX_ACTIVE_SESSIONS` caps how many devices one account may be signed in on. A login beyond it is refused with `409 SESSION_LIMIT_REACHED`. That refusal is raised **after** the password has been verified, so the API may safely list the account's own open sessions in the error body (id, login time, last activity, IP, user agent — no token and no hash) and the login screen shows them instead of a dead end. When `SECURITY_CONFIG.SESSION_FULL_BEHAVIOUR` is `REJECT_ALLOW_EVICT`, each row offers "sign out and continue", which re-sends the same login with `endSessionId`; with `REJECT` the list is read-only and the user must log out on the other device.

The tradeoff is deliberate and worth stating: someone who already holds a valid password can now end a legitimate session rather than being stopped by the limit. They could not have been stopped for long anyway — the limit delays a thief, it does not authenticate anyone — and the exchange buys a real reduction in password sharing, because a blocked user's usual workaround is to hand their credentials to whoever is holding the other session. Every eviction is written to `USER_LOG` as `SESSION_REVOKED` with the actor and the originating IP, so the real owner sees when and from where their session was ended. An installation that prefers the hard limit sets `SESSION_FULL_BEHAVIOUR` back to `REJECT` in the admin panel; that is also the code default, so a missing or unrecognised value is the strict behaviour.

The API change that implements this is specified in [backend-changes.md](backend-changes.md). The ESLint rules `no-restricted-globals` / `no-restricted-properties` block `localStorage`, `sessionStorage` and `document.cookie` in application code so the invariant cannot erode.

## CSRF

Two independent layers. The cookie is `SameSite=Lax`, so a cross-site form post or fetch does not carry it. In addition every request sends `X-Requested-With: XMLHttpRequest` (`csrf.interceptor.ts`); a browser adds a custom header to a cross-origin request only after a CORS preflight, which the API's CORS policy refuses for foreign origins, so a forged request can never carry the header. The API rejects state-changing requests without it (`403 CSRF_REJECTED`) and checks the `Origin` header on the login endpoint.

## XSS

React escapes everything it renders. `dangerouslySetInnerHTML`, `innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`, `eval`, `new Function` and `javascript:` URLs are ESLint errors. No third-party script is loaded; MUI icons are inlined SVG. If HTML from the API ever has to be rendered (a report), it goes through a single `SafeHtml` component in `shared/` that sanitises with DOMPurify — that component does not exist yet, on purpose.

The Content Security Policy is sent by the API as a response header (never a `<meta>` tag, which injected markup could precede). The recommended policy and the reason MUI needs `style-src 'unsafe-inline'` are in [backend-changes.md](backend-changes.md); a nonce-based policy is the planned next step.

## Injection

The front end builds no SQL, no HTML and no shell commands. Path parameters are `encodeURIComponent`-ed in the API functions. Query parameters go through Axios serialisation. Search text is length-capped before it reaches the URL and the API.

## Authorization

Rights come from the API (`permissions` in `/api/auth/me`) as a closed set of keys. The UI uses them to hide or disable, in exactly one way each: `createProtectedRoute({ permission })` for routes, `<Can right>` for elements, `usePermission` for logic, and `menu.config.ts` for the sidebar. A missing UI check is a bug; a missing API check is a security incident. Never derive rights from a role name or id in the UI.

## Open redirects

The only redirect the app performs is the post-login return. `safeRedirectPath` accepts an absolute same-origin path and nothing else (`//host`, `https://`, `javascript:`, control characters and the login page itself all fall back to the dashboard). The target travels in router state, not in the URL.

## Sensitive information

Log entries are redacted before any transport sees them: keys matching password, token, jwt, secret, answer, hash, cookie, authorization and similar are replaced with `***`, strings are truncated and errors are reduced to name and message. Only ids and codes are logged, never names, phone numbers, e-mail addresses or request/response bodies. Production source maps are hidden (generated for symbolication, never referenced by the bundle) and must not be deployed to the web root. `VITE_*` variables are public by definition; a secret in `.env*` is a defect, and `env.ts` refuses a production build with mocks enabled or a plain-http API URL.

## Error handling

Users see a short message and a correlation reference, never a stack trace. The API's `stackTrace` and `exceptionType` members are parsed out and ignored (`problem-details.ts`). Route-level error boundaries keep the shell usable; the global boundary shows a reload page.

## Dependencies and supply chain

Exact versions for framework-level packages, caret for the rest, and `npm ci` from a committed lockfile with integrity hashes. Install scripts are disabled (`ignore-scripts=true` in `.npmrc`), which removes the most common vector for a malicious package to run code on a developer or CI machine. Third-party packages are the smallest set that covers the requirements (see [dependencies.md](dependencies.md)); every addition needs a stated reason in the PR. CI fails on high or critical advisories (`npm audit`, GitHub dependency review) and Dependabot opens grouped weekly PRs for patch and minor updates. Major upgrades are deliberate and recorded in an ADR.

## Secure HTTP configuration

The production bundle is served by the API from `wwwroot`, so the browser sees one origin: the session cookie is first-party, CORS is not needed and the dev-server proxy reproduces the same shape locally. TLS terminates at the API (Kestrel) or at the reverse proxy in front of it; HSTS, `X-Content-Type-Options`, `X-Frame-Options`/`frame-ancestors`, `Referrer-Policy`, `Permissions-Policy` and the CSP are set by the API (see backend-changes.md). Static assets get long-lived immutable caching (content-hashed names); `index.html` is `no-store`.

## Testing security behaviour

Unit tests cover the ProblemDetails mapping, the redirect validator, log redaction and the session rules of the HTTP client (session ended, silent 401, hard-limit park-and-retry, cancel). Component tests cover the login outcomes (wrong password, locked, forced change) and rights-based rendering. Browser tests cover login, deep-link redirect and sign-out. Add a test with every security-relevant fix.
