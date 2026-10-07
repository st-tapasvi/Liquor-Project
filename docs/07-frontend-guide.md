# Frontend Guide — Login, Supplier Codes, Roles & Rights

For the developer of the React app (`ST.LiquorTNT.WebApplication`).
**Part A** is a short summary for you. **Part B** is the full reference, written so an AI assistant can build the screens
from it. Give Part B to the AI as it is.

Backend version: 2026-10-07. Everything here already works on the dev server.

---

# PART A — SUMMARY (read this)

## A1. Five words you need

| Word | Meaning |
|---|---|
| **Company** | The customer, e.g. *Globus Spirits Ltd*. Every user (except Super Admin) belongs to one company. |
| **Supplier code** | A code the excise department gives the company, per state and liquor category, e.g. **RJ CL 772**, **RJ IMFL 1028**, **JK IMFL 369**. A company has several. The user always works **inside one supplier code at a time**. |
| **Role** | A named set of rights of the company (Plant Admin, Agent Manager, Plant Manager, Supervisor, Operator, Viewer). A user can have **several roles**, each for one supplier code or for **all** supplier codes. |
| **Right / permission key** | One thing a user may do, written `page.action`, e.g. `user.add`, `role.edit`. The server sends the list of keys the user holds in the selected supplier code. |
| **Super Admin** | Sundaram Tech's own user (`admin`). Has every right in every company. |

## A2. The flow of the app

```
1. Login page          POST /api/auth/login
                       └─ response has: user, supplierCodes[], activeSupplierCode
2. Supplier code       if activeSupplierCode is null → show a picker with supplierCodes[]
   picker              POST /api/auth/selectsuppliercode { supplierCodeId }
3. App start           GET /api/auth/mypermissions → { permissions[], activeSupplierCode }
                       build the menu and buttons from permissions[]
4. Header              show the active supplier code ("RJ CL 772"), with a "switch" dropdown
                       (same picker → selectsuppliercode → reload mypermissions)
5. Screens             Users, Roles, Supplier Codes, Liquor Categories, Security settings …
                       each button is shown only if its permission key is in permissions[]
6. Errors              handled once, in the axios interceptor (see A4)
```

The browser login uses an **HttpOnly cookie** (`jwt`). You never store or send the token yourself; just use
`withCredentials: true` and let axios send the `X-XSRF-TOKEN` header (one-time axios setup, Part B §2).

## A3. Screens to build

| Screen | What it does | Main APIs |
|---|---|---|
| Login | user name + password; first-login password change; forgot password | `auth/login`, `auth/changepassword`, `auth/forgotpassword/*` |
| Supplier code picker | after login (and from the header); shows **only** the supplier codes the user holds | `auth/mysuppliercodes`, `auth/selectsuppliercode` |
| Users | list, create (with roles), edit profile, activate / deactivate, unlock | `/api/users…` |
| User access | a user's roles (per supplier code) and extra "custom" rights | `/api/users/{id}/access`, `/roles`, `/rights` |
| Roles | list, create, edit, delete | `/api/roles…` |
| Role rights | a grid: pages × actions with checkboxes, one Save | `/api/roles/{id}/rights`, `/api/pages` |
| Supplier codes | list for the company; Super Admin can add / edit | `/api/suppliercodes…` |
| Liquor categories | list; Super Admin can add / edit | `/api/liquorcategories…` |
| Security settings / Password policies | admin settings (only for admins) | `/api/securityconfig`, `/api/passwordpolicies` |

## A4. Rules that matter

1. **Show / hide by permission key**, e.g. show "Add user" only if `permissions` contains `user.add`. The server checks every
   call anyway; hiding is just for a clean screen.
2. **The user never sends company or supplier code** to any API (except the picker). The server knows it from the session.
3. **Errors are handled by `errorCode`**, never by message text:
   - `SUPPLIER_CODE_NOT_SELECTED` (409) → open the supplier code picker
   - `PERMISSION_DENIED` (403) → "You don't have the right to do this" (`detail` names the missing right)
   - `SESSION_EXPIRED` (401) → password popup, log in again, retry the call
   - `SESSION_TIMED_OUT` / `SESSION_INVALID` / `UNAUTHENTICATED` (401) → go to the login page
   - `VALIDATION_FAILED` (400) → show `errors[field]` under each form field
4. **Lists that are saved as a whole** (role rights, user roles, user custom rights): send the **full** list on Save; anything
   not in the list is removed.
5. A rights change by an admin applies on the user's **next call**; no new login. Re-read `mypermissions` after the user
   switches supplier code, and on page refresh.
6. **The picker never shows a supplier code the user has no role or right on.** Example: the company has RJ 550, RJ 560 and
   RJ 750; user A has roles only on RJ 550 and RJ 560 -> `mysuppliercodes` returns those two, RJ 750 does not appear, and if
   some client sends its id anyway the server answers `403 SUPPLIER_CODE_NOT_ASSIGNED`. An excise with none of the user's
   supplier codes therefore never appears either. A role given for "all supplier codes" covers every supplier code of the company.

## A5. Test users (dev server, password `Admin@123` for all)

| User | Role | Supplier codes | After login |
|---|---|---|---|
| `admin` | Super Admin | all companies | picker with every supplier code |
| `globus.admin` | Plant Admin | all 3 | picker |
| `globus.agent` | Agent Manager | all 3 | picker |
| `globus.pm` | Plant Manager | all 3 | picker |
| `rj.supervisor` | Supervisor | RJ 772, RJ 1028 | picker (2) |
| `rj772.operator` | Operator (+ extra right `user.view`) | RJ 772 | selected automatically |
| `rj1028.operator` | Operator | RJ 1028 | selected automatically |
| `jk369.operator` | Operator | JK 369 | selected automatically |
| `globus.viewer` | Viewer | all 3 | picker |

---

# PART B — DETAILED REFERENCE (for the AI assistant)

## B1. Basics

- **Base URL (dev):** `https://localhost:7180` or `http://localhost:5180`. All paths start with `/api/`.
  Swagger: `/swagger`.
- **JSON** is camelCase. **Dates/times** are Indian time (IST) without a time zone, e.g. `"2026-10-07T15:15:35"`. Show them as
  they are; do not convert.
- **Every response has a body.** Success returns the object itself (no `{ success, data }` wrapper). An action with nothing to
  return answers `{ "message": "..." }`. Lists that page return `{ items, page, pageSize, totalCount }`.
- **Routes** are lowercase without `-`: `/api/auth/selectsuppliercode`.

## B2. Authentication (cookie) — axios setup

Login (`POST /api/auth/login`) sets two cookies:
- `jwt` — HttpOnly (JavaScript cannot read it). The browser sends it with every request.
- `XSRF-TOKEN` — readable. On every POST / PUT / DELETE, its value must be sent in the header `X-XSRF-TOKEN`.

Logout clears both. The desktop app and Swagger use `Authorization: Bearer <accessToken>` instead; the web app does **not**
need the token from the body.

```ts
import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,   // e.g. https://localhost:7180
  withCredentials: true,                    // send the jwt cookie
  xsrfCookieName: "XSRF-TOKEN",             // axios reads this cookie ...
  xsrfHeaderName: "X-XSRF-TOKEN",           // ... and sends it on POST/PUT/DELETE
  withXSRFToken: true,                      // axios >= 1.6: also for a different origin in dev
});
```

If the dev server runs the React app on another port, use the Vite proxy (same origin) or make sure the API's
`Cors:AllowedOrigins` contains the app's origin.

## B3. Error format (every error)

```json
{
  "type": "https://errors.stliquortnt.local/permission_denied",
  "status": 403,
  "errorCode": "PERMISSION_DENIED",
  "title": "You do not have the right to do this.",
  "detail": "This action needs the permission 'user.add' in the selected supplier code. Ask your administrator …",
  "instance": "/api/users",
  "correlationId": "6f1c…",
  "errors": { "userName": ["…"] }          // only for 400 VALIDATION_FAILED, keyed by camelCase field
}
```

Show `title` (and `detail` where useful). Always show `correlationId` on unexpected errors, so support can find the log.

### Global handling (one axios response interceptor)

| errorCode | Status | What the app does |
|---|---|---|
| `UNAUTHENTICATED`, `SESSION_INVALID`, `SESSION_TIMED_OUT` | 401 | clear local user state → login page |
| `SESSION_EXPIRED` | 401 | keep the screen; password popup; `POST /api/auth/login` with the same user name; then **retry the failed call** (queue other calls that fail meanwhile) |
| `SUPPLIER_CODE_NOT_SELECTED` | 409 | open the supplier code picker; after selection, reload `mypermissions` and retry |
| `PERMISSION_DENIED` | 403 | toast "You don't have the right …"; the page stays |
| `CSRF_REJECTED` | 403 | a client bug: the `X-XSRF-TOKEN` header is missing — check the axios setup |
| `VALIDATION_FAILED` | 400 | show `errors[field]` under the form fields |
| `NOT_FOUND` | 404 | "Not found" (also returned for records of another company) |
| `DATABASE_ERROR`, `UNEXPECTED_ERROR` | 500 | "Something went wrong" + `correlationId` |

Screen-specific codes are listed with each API below.

## B4. App start and the supplier code

```
on login success (LoginResponse):
   save user (not the token)
   if response.activeSupplierCode == null:
        if response.supplierCodes is empty → message "No supplier code is assigned to you. Contact your administrator."
                                              (Super Admin may still open company-independent screens)
        else → show picker(response.supplierCodes)
   else → loadPermissions()

on page refresh / app start:
   GET /api/auth/me            → 401 = not logged in → login page
   GET /api/auth/mypermissions → if activeSupplierCode == null → GET /api/auth/mysuppliercodes → picker

picker.onSelect(id):
   POST /api/auth/selectsuppliercode { supplierCodeId: id }  → MyPermissionsResponse (use it directly)

header switcher: same picker, list from GET /api/auth/mysuppliercodes
```

Keep `permissions` in a global store (Context / Redux / Zustand) as a `Set<string>`:
`const can = (key: string) => isSuperAdmin || permissions.has(key);`

## B5. TypeScript types

```ts
// ---------- common ----------
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; }
export interface MessageResponse { message: string; }
export interface ProblemDetails {
  type: string; status: number; errorCode: string; title: string; detail?: string;
  instance: string; correlationId: string; errors?: Record<string, string[]>;
}

// ---------- auth ----------
export interface LoginRequest { userName: string; password: string; }
export interface CurrentUserResponse {
  userId: number; userName: string; fullName?: string | null; companyId?: number | null;
  forcePasswordChange: boolean; passwordExpiresAt?: string | null;
}
export interface LoginResponse {
  accessToken: string;            // ignore in the web app (the cookie is used)
  expiresAt: string;              // hard limit of the session (IST)
  idleTimeoutMinutes: number;     // no call for this long → SESSION_TIMED_OUT
  user: CurrentUserResponse;
  supplierCodes: SupplierCodeResponse[];
  activeSupplierCode: SupplierCodeResponse | null;
}
export interface ChangePasswordRequest { userName: string; currentPassword: string; newPassword: string; }
export interface SessionResponse {
  id: number; loginAt: string; lastActivityAt?: string | null; expiresAt: string;
  ipAddress?: string | null; userAgent?: string | null; isCurrent: boolean;
}

// ---------- access ----------
export interface SelectSupplierCodeRequest { supplierCodeId: number; }
export interface MyPermissionsResponse {
  isSuperAdmin: boolean;
  activeSupplierCode: SupplierCodeResponse | null;
  permissions: string[];          // e.g. ["role.view", "user.add", ...]
}

// ---------- supplier codes / liquor categories ----------
export interface SupplierCodeResponse {
  id: number; companyId: number; companyName?: string | null; franchiseName?: string | null;
  exciseId: number; exciseCode: string;           // "RJ"
  supplierCode: string;                            // "772"
  liquorCategoryId: number; liquorCategoryCode: string;   // "CL"
  displayName: string;                             // "RJ CL 772" — use this in pickers and headers
  isActive: boolean; createdAt?: string | null;
}
export interface CreateSupplierCodeRequest {
  companyId: number; franchiseName?: string | null; exciseId: number; supplierCode: string; liquorCategoryId: number;
}
export interface UpdateSupplierCodeRequest {
  franchiseName?: string | null; exciseId: number; supplierCode: string; liquorCategoryId: number;
}
export interface LiquorCategoryResponse {
  id: number; categoryCode: string; categoryName: string; description?: string | null; isActive: boolean;
}
export interface SaveLiquorCategoryRequest { categoryCode: string; categoryName: string; description?: string | null; }

// ---------- roles and rights ----------
export interface RoleResponse {
  id: number; companyId?: number | null; roleName: string; description?: string | null;
  isSystem: boolean;      // Super Admin: read-only, never editable
  isTemplate: boolean;    // default template (only Super Admin sees these)
  isAdminRole: boolean;   // Plant Admin: only users with user.manageadmin may edit/assign it
  isActive: boolean; passwordPolicyId?: number | null;
}
export interface SaveRoleRequest { roleName: string; description?: string | null; isAdminRole: boolean; passwordPolicyId: number; }
export type GrantScope = "ANY" | "ADMIN" | "SYSTEM";
export interface PageActionResponse {
  pageActionId: number; actionKey: string; permissionKey: string; actionName: string;
  grantScope: GrantScope; granted: boolean;
}
export interface PageResponse { pageId: number; pageKey: string; pageName: string; moduleName?: string | null; actions: PageActionResponse[]; }
export interface RoleRightsResponse { roleId: number; roleName: string; pages: PageResponse[]; }
export interface UpdateRoleRightsRequest { pageActionIds: number[]; }   // the FULL ticked list

// ---------- users ----------
export interface UserRoleAssignment { roleId: number; supplierCodeId: number | null; }   // null = all supplier codes
export interface UserRightAssignment { pageActionId: number; supplierCodeId: number | null; }
export interface CreateUserRequest {
  userName: string; password: string;
  companyId?: number | null;          // only Super Admin sends it; ignored for everyone else
  roles: UserRoleAssignment[];        // at least one
  fullName?: string; email?: string; phone?: string; employeeCode?: string;
  forcePasswordChange: boolean;       // default true
}
export interface UpdateUserRequest { fullName?: string; email?: string; phone?: string; employeeCode?: string; }
export interface UserResponse {
  id: number; userName: string; fullName?: string | null; email?: string | null; phone?: string | null;
  employeeCode?: string | null; companyId?: number | null; isActive: boolean; isBlocked: boolean;
  failedLoginAttempts: number; lockedUntil?: string | null; forcePasswordChange: boolean;
  passwordExpiresAt?: string | null; lastLoginAt?: string | null; createdAt?: string | null;
}
export interface UserRoleResponse { roleId: number; roleName: string; supplierCodeId: number | null; supplierCodeName: string; } // "RJ CL 772" or "All supplier codes"
export interface UserRightResponse { pageActionId: number; permissionKey: string; actionName: string; supplierCodeId: number | null; supplierCodeName: string; }
export interface UserAccessResponse { userId: number; userName: string; roles: UserRoleResponse[]; rights: UserRightResponse[]; }

// ---------- dropdown lists ----------
export interface ExciseResponse { id: number; exciseCode: string; exciseName: string; isActive: boolean; }   // "RJ", "Rajasthan"
export interface CompanyResponse {
  id: number; companyName?: string | null; aliasName?: string | null; city?: string | null; isActive: boolean;
  supplierCodeCount: number;          // 0 = no supplier code yet (and so no default roles yet)
}

// ---------- admin settings ----------
export interface SecurityConfigResponse { key: string; value: string; dataType: "INT" | "BOOL" | "STRING"; description?: string | null; updatedAt: string; }
export interface PasswordPolicyResponse {
  id: number; policyName: string; minLength: number; maxLength: number;
  requireUppercase: boolean; requireLowercase: boolean; requireNumber: boolean; requireSpecialCharacter: boolean;
  passwordHistoryCount: number; passwordExpiryEnabled: boolean; passwordExpiryDays?: number | null;
  allowUsernameInPassword: boolean; allowCommonPassword: boolean; status: boolean; updatedAt: string;
}
```

## B6. Permission keys (what each one unlocks)

| Key | Unlocks | Who may give it |
|---|---|---|
| `user.view` | Users list, user details, user access view | anyone managing roles |
| `user.add` | "Add user" | 〃 |
| `user.edit` | "Edit" user profile | 〃 |
| `user.status` | "Activate" / "Deactivate" | 〃 |
| `user.unlock` | "Unlock" | 〃 |
| `user.access` | "Edit roles" / "Edit custom rights" of a user | 〃 |
| `user.manageadmin` | managing **admin users** (Plant Admin holders), admin roles, ADMIN rights | admins only (`ADMIN`) |
| `role.view` | Roles list, role rights grid, pages list | anyone |
| `role.add` / `role.edit` / `role.delete` | "Add role" / "Edit role + rights" / "Delete role" | anyone |
| `securityconfig.view` / `securityconfig.edit` | Security settings screen | admins only |
| `passwordpolicy.view` / `passwordpolicy.edit` | Password policies screen | admins only |
| `suppliercode.view` | Supplier codes list | anyone |
| `suppliercode.view` (also) | the excise list (`GET /api/excises`) | anyone |
| `suppliercode.add` / `suppliercode.edit` | Add / edit / (de)activate supplier code | **nobody** — Super Admin only (`SYSTEM`) |
| `company.view` | Company list (`GET /api/companies`) | **nobody** — Super Admin only (`SYSTEM`) |
| `liquorcategory.view` | Liquor categories list | anyone |
| `liquorcategory.add` / `liquorcategory.edit` | Add / edit / (de)activate category | **nobody** — Super Admin only (`SYSTEM`) |

New pages (batch, plan …) will add more keys later; the screens below pick them up automatically from `/api/pages`.

Menu suggestion: Administration (Users → `user.view`, Roles → `role.view`, Security settings → `securityconfig.view`,
Password policies → `passwordpolicy.view`), Masters (Supplier codes → `suppliercode.view`, Liquor categories →
`liquorcategory.view`). Hide a menu entry when its key is missing.

## B7. APIs — login and account

### `POST /api/auth/login` (public)
Request `LoginRequest`, response `LoginResponse` (sets the cookies). Example:
```json
{
  "accessToken": "eyJ…",
  "expiresAt": "2026-10-08T15:13:12",
  "idleTimeoutMinutes": 60,
  "user": { "userId": 234, "userName": "rj.supervisor", "fullName": "Suresh Yadav", "companyId": 20,
            "forcePasswordChange": false, "passwordExpiresAt": null },
  "supplierCodes": [
    { "id": 30, "companyId": 20, "companyName": "Globus Spirits Ltd", "franchiseName": null, "exciseId": 2,
      "exciseCode": "RJ", "supplierCode": "772", "liquorCategoryId": 2, "liquorCategoryCode": "CL",
      "displayName": "RJ CL 772", "isActive": true, "createdAt": "2026-10-07T15:15:35" },
    { "id": 31, "displayName": "RJ IMFL 1028", "…": "…" }
  ],
  "activeSupplierCode": null
}
```
| Error | Status | Screen action |
|---|---|---|
| `VALIDATION_FAILED` | 400 | field errors |
| `INVALID_CREDENTIALS` | 401 | "Wrong user name or password" |
| `USER_LOCKED` | 403 | show `detail` (contains unlock time) |
| `USER_INACTIVE` / `USER_BLOCKED` | 403 | "Contact your administrator" |
| `PASSWORD_CHANGE_REQUIRED` / `PASSWORD_EXPIRED` | 403 | open "Set new password" → `changepassword` → login again |
| `SESSION_LIMIT_REACHED` | 409 | "Already logged in on the maximum number of devices — log out elsewhere" |

### `POST /api/auth/logout` (logged in) → `MessageResponse`, clears cookies.
### `GET /api/auth/me` (logged in) → `CurrentUserResponse`. Use it on page refresh to know if the user is logged in.
### `POST /api/auth/changepassword` (public, verified by the current password)
Request `ChangePasswordRequest` → `MessageResponse`. Ends all sessions; log in again. `400` with `errors.password` lists every
broken rule of the user's password policy.
### `GET /api/auth/sessions` → `SessionResponse[]` · `DELETE /api/auth/sessions/{id}` → `MessageResponse` (log out another device).
### Forgot password (public)
1. `POST /api/auth/forgotpassword/start` `{ userName }` → `{ requestToken, questionText, expiresAt }`
2. `POST /api/auth/forgotpassword/verify` `{ requestToken, answer }` → `MessageResponse`
3. `POST /api/auth/forgotpassword/reset` `{ requestToken, newPassword }` → `MessageResponse`, then login.
Errors: `SECURITY_QUESTION_NOT_SET` / `SECURITY_QUESTION_DISABLED` (409), `SECURITY_ANSWER_INCORRECT` (401),
`RESET_ATTEMPTS_EXCEEDED` (403), `RESET_REQUEST_EXPIRED` / `RESET_REQUEST_INVALID` (409).
### Security question of the user
`GET /api/securityquestions` (public) → `SecurityQuestionResponse[]` (`{ id, questionText }`);
`PUT /api/securityquestions/mine` `{ questionId, answer, currentPassword }` → `MessageResponse`.

## B8. APIs — supplier code of the session

| Call | Response | Notes |
|---|---|---|
| `GET /api/auth/mysuppliercodes` | `SupplierCodeResponse[]` | for the picker / header switcher — **only the supplier codes this user holds** (Super Admin: all active ones of every company) |
| `POST /api/auth/selectsuppliercode` `{ supplierCodeId }` | `MyPermissionsResponse` | picks or switches. `403 SUPPLIER_CODE_NOT_ASSIGNED` if not the user's |
| `GET /api/auth/mypermissions` | `MyPermissionsResponse` | `permissions` is empty while no supplier code is picked (except Super Admin) |

Picker UI: a list or dropdown of `displayName`, grouped by `exciseCode` if there are many. The list already contains only the user's own supplier codes, so build the excise grouping from it (do **not** call `/api/excises` for the picker): an excise with no supplier code of the user simply has no group. Super Admin sees supplier codes of
every company — show `companyName` too.

## B9. APIs — roles and the rights grid

| Call | Right | Request → Response |
|---|---|---|
| `GET /api/roles` | `role.view` | → `RoleResponse[]` (the company's roles) |
| `GET /api/roles/{id}` | `role.view` | → `RoleResponse` |
| `POST /api/roles` | `role.add` | `SaveRoleRequest` → `201 RoleResponse` |
| `PUT /api/roles/{id}` | `role.edit` | `SaveRoleRequest` → `RoleResponse` |
| `DELETE /api/roles/{id}` | `role.delete` | → `MessageResponse` |
| `GET /api/roles/{id}/rights` | `role.view` | → `RoleRightsResponse` (every page and action, `granted` ticked) |
| `PUT /api/roles/{id}/rights` | `role.edit` | `UpdateRoleRightsRequest` (FULL ticked list) → `RoleRightsResponse` |
| `GET /api/pages` | `role.view` | → `PageResponse[]` (empty grid; for the user custom-rights screen) |

**Roles list screen:** table of `roleName`, `description`, badges for `isAdminRole` ("Admin") and `isSystem` ("System");
buttons Edit / Rights / Delete by key. Disable Edit / Rights / Delete when `isSystem` is true.

**Role form:** `roleName` (required, ≤100), `description` (≤255), `isAdminRole` checkbox (show only if `user.manageadmin`),
`passwordPolicyId` dropdown from `GET /api/passwordpolicies` (needs `passwordpolicy.view`; see B13 known gap).

**Rights grid screen** (`GET /api/roles/{id}/rights`):
- Rows = pages (group by `moduleName`), columns/chips = that page's actions (pages have different actions).
- Checkbox per action = `granted`. "Select all" per row is handy.
- `grantScope = "SYSTEM"` → always disabled (Super Admin only). `grantScope = "ADMIN"` → disabled unless the user has
  `user.manageadmin`.
- Save = `PUT` with the ids of **all** ticked actions.

| Error | Status | Meaning |
|---|---|---|
| `ROLE_NAME_TAKEN` | 409 | name already used in this company |
| `ROLE_IN_USE` | 409 | delete refused: users still hold the role |
| `ROLE_NOT_EDITABLE` | 409 | Super Admin role / templates |
| `ADMIN_USER_PROTECTED` | 403 | admin role or ADMIN right without `user.manageadmin` |
| `RIGHT_NOT_GRANTABLE` | 403 | tried to tick a SYSTEM right |
| `NOT_FOUND` | 404 | role of another company, or unknown password policy |

## B10. APIs — users and user access

| Call | Right | Request → Response |
|---|---|---|
| `GET /api/users?search=&page=1&pageSize=50` | `user.view` | → `PagedResponse<UserResponse>` (company's users; pageSize max 200) |
| `GET /api/users/{id}` | `user.view` | → `UserResponse` |
| `POST /api/users` | `user.add` | `CreateUserRequest` → `201 UserResponse` |
| `PUT /api/users/{id}` | `user.edit` | `UpdateUserRequest` (profile only; send all fields) → `UserResponse` |
| `POST /api/users/{id}/activate` · `/deactivate` | `user.status` | → `UserResponse` |
| `POST /api/users/{id}/unlock` | `user.unlock` | → `UserResponse` |
| `GET /api/users/{id}/access` | `user.view` | → `UserAccessResponse` |
| `PUT /api/users/{id}/roles` | `user.access` | `{ roles: UserRoleAssignment[] }` (FULL list, ≥1) → `UserAccessResponse` |
| `PUT /api/users/{id}/rights` | `user.access` | `{ rights: UserRightAssignment[] }` (FULL list, may be empty) → `UserAccessResponse` |

**Create user form:** userName (letters, digits `. _ @ -`, ≤50, cannot change later), password, fullName, email, phone,
employeeCode, forcePasswordChange (default on), and a **roles section**: rows of *Role* (from `GET /api/roles`) ×
*Supplier code* (from `GET /api/suppliercodes`, plus an "All supplier codes" option = `null`). Super Admin also picks the
company (`companyId`).

**User access screen** (`GET /api/users/{id}/access`):
- Section "Roles": rows `roleName` + `supplierCodeName`; add / remove rows; Save → `PUT …/roles` with the full list.
- Section "Custom rights" (extra rights on top of roles; they never remove a role's right): rows *Right* (pick from
  `GET /api/pages` actions, show `pageName → actionName`) × *Supplier code* (or All); Save → `PUT …/rights` with the full list.
  Disable SYSTEM actions; disable ADMIN actions unless the user has `user.manageadmin`.
- Hide both Save buttons when viewing **yourself** (the server refuses: `CANNOT_CHANGE_OWN_ACCESS`), unless Super Admin.

| Error | Status | Meaning |
|---|---|---|
| `USERNAME_TAKEN` | 409 | user name exists |
| `PASSWORD_POLICY_NOT_CONFIGURED` | 409 | a chosen role has no password policy |
| `VALIDATION_FAILED` | 400 | `errors.password` lists broken password rules; `errors.roles` when no role |
| `ADMIN_USER_PROTECTED` | 403 | target is an admin user / admin role / ADMIN right, caller lacks `user.manageadmin` |
| `RIGHT_NOT_GRANTABLE` | 403 | SYSTEM right in custom rights |
| `CANNOT_CHANGE_OWN_ACCESS` | 409 | editing your own roles / rights |
| `CANNOT_DEACTIVATE_SELF` | 409 | deactivating yourself |
| `NOT_FOUND` | 404 | user / role / supplier code of another company |

## B11. APIs — supplier codes and liquor categories

| Call | Right | Request → Response |
|---|---|---|
| `GET /api/suppliercodes?search=&page=1&pageSize=50` | `suppliercode.view` | → `PagedResponse<SupplierCodeResponse>` (company's; Super Admin: all) |
| `GET /api/suppliercodes/{id}` | `suppliercode.view` | → `SupplierCodeResponse` |
| `POST /api/suppliercodes` | `suppliercode.add` (Super Admin) | `CreateSupplierCodeRequest` → `201` |
| `PUT /api/suppliercodes/{id}` | `suppliercode.edit` (Super Admin) | `UpdateSupplierCodeRequest` → `SupplierCodeResponse` |
| `POST /api/suppliercodes/{id}/activate` · `/deactivate` | `suppliercode.edit` | → `SupplierCodeResponse` |
| `GET /api/liquorcategories` | `liquorcategory.view` | → `LiquorCategoryResponse[]` |
| `GET /api/liquorcategories/{id}` | `liquorcategory.view` | → `LiquorCategoryResponse` |
| `POST /api/liquorcategories` · `PUT /api/liquorcategories/{id}` | `liquorcategory.add` / `edit` | `SaveLiquorCategoryRequest` |
| `POST /api/liquorcategories/{id}/activate` · `/deactivate` | `liquorcategory.edit` | → `LiquorCategoryResponse` |
| `GET /api/excises` | `suppliercode.view` | → `ExciseResponse[]` (every excise; show active ones in dropdowns) |
| `GET /api/companies` | `company.view` (Super Admin) | → `CompanyResponse[]` (by name, with `supplierCodeCount`) |

Supplier code form (Super Admin): company (from `GET /api/companies`), excise (from `GET /api/excises`, active only), supplier code (≤20),
liquor category (from `/api/liquorcategories`, active only), franchise name (optional text). The first supplier code of a
company also creates the company's default roles. Errors: `SUPPLIER_CODE_TAKEN` (409, same code in the same excise),
`LIQUOR_CATEGORY_CODE_TAKEN` (409), `NOT_FOUND` (company / excise / category).


## B12. APIs — admin settings

| Call | Right |
|---|---|
| `GET /api/securityconfig` → `SecurityConfigResponse[]` | `securityconfig.view` |
| `PUT /api/securityconfig/{key}` `{ value }` → `SecurityConfigResponse` | `securityconfig.edit` |
| `GET /api/passwordpolicies` → `PasswordPolicyResponse[]` | `passwordpolicy.view` |
| `PUT /api/passwordpolicies/{id}` (all rule fields) → `PasswordPolicyResponse` | `passwordpolicy.edit` |

Render each setting by `dataType`: BOOL → switch, INT → number input, STRING → text.

## B13. Known gaps (tell the backend team if they block you)

- **Password policy dropdown on the role form** needs `passwordpolicy.view`, which by default only Plant Admin has. An Agent
  Manager can create roles but cannot load that list. Until the backend adds a light list for role editing, show the dropdown
  only when `passwordpolicy.view` is present, and otherwise default to MEDIUM (id 2 on the dev server).
- **Batch and other production pages** are not built yet. When they come, they add their keys to `/api/pages` and
  `mypermissions`; the generic screens above need no change.
