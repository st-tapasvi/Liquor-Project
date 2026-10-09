# Frontend Guide — Login, Users, Roles & Settings

For the React team (and the AI assistant you use). It covers everything needed for the first set of screens:

1. Login, first-login password change, security question, forgot password
2. Users: list, create (with a temporary password), edit, activate / deactivate, unlock
3. User access: master roles + custom rights
4. Master roles: create, edit, delete, and the rights grid
5. Settings: the security settings page (and password policies)

Every request and response below was captured from the running API (dev database, 2026-10-08). Long lists are shortened with `…`.

**How to read this:** Part A explains the idea and the flows; give it to the person building the screens. Part B has
every API with request, response and errors; give it to the AI assistant as the reference.

---

# PART A — THE IDEA AND THE FLOWS

## A1. Words you need

| Word | Meaning |
|---|---|
| **Company** | The customer, e.g. *Globus Spirits Ltd*. Companies already exist in the database (they come from our CRM); there is no "create company" screen. |
| **Supplier code** | A code the state excise department gives the company, per state and liquor category: **RJ CL 772**, **RJ IMFL 1028**, **JK IMFL 369**. A company has several. It is shown as `excise + category + code`. The same number can exist in two states (RJ 772 and JK 772); they are different supplier codes with different ids. |
| **Session supplier code** | After **every** login, **every** user (also with only one supplier code) goes to the supplier code screen and picks one; then the dashboard opens. The user works inside that one supplier code (switchable from the header). Rights and data follow it. |
| **Master role** | A named set of rights. **Each supplier code has its own roles** ("Operator RJ CL 772", "Operator RJ IMFL 1028"), so each can get different rights. **Company-level roles** (Plant Admin, Agent Manager) cover every supplier code of the company. A user can hold several roles. |
| **Right (permission key)** | One thing a user may do on one page, written `page.action`: `user.add`, `role.edit`. Pages and their actions are fixed by us (seeded in the database). |
| **Role group** | A named bundle of master roles, given to users as one piece ("All Operators" = Operator RJ CL 772 + Operator RJ IMFL 1028). Change the group once and every user of it changes. Optional: roles can still be given directly. |
| **Custom right** | A right given straight to one user, on top of the roles (for one supplier code or all). It only adds; it never takes a role's right away. |
| **Admin** | The `admin` user of this installation (system role "Admin"): every right in every company. Not linked to a company. Not the same as a company's **Plant Admin** role. |

## A2. The whole flow, start to end

```
NEW INSTALLATION
 1. Admin logs in: admin / Admin@123 ............... 403 PASSWORD_CHANGE_REQUIRED
 2. "Set new password" screen ............................ POST /api/auth/changepassword
 3. Logs in with the new password ........................ 200, user.securityQuestionRequired = true
 4. "Set security question" screen ....................... PUT /api/securityquestions/mine
 5. Supplier code screen: picks a supplier code ......... POST /api/auth/selectsuppliercode
    -> the company of that supplier code is now the company for the screens below
 6. Dashboard; menu from the permissions in that response

ADMIN SETS UP THE COMPANY
 7. Roles screen: the company already has its default roles (created with each supplier code):
       Plant Admin, Agent Manager                  (company-level)
       Plant Manager / Supervisor / Operator / Viewer  RJ CL 772
       Plant Manager / Supervisor / Operator / Viewer  RJ IMFL 1028   ...
    Admin may edit their rights (rights grid), create new roles ("Packer RJ CL 772"), delete unused ones.
 8. Role groups screen (optional): e.g. "All Operators" = Operator RJ CL 772 + Operator RJ IMFL 1028.
 9. Users screen: creates a user with a TEMPORARY password and a role group and/or roles.
10. User access screen: changes groups and roles, adds custom rights.
11. Settings screen: security settings (lockout, sessions, security question, reset, log mode).

THE NEW USER
12. Logs in with the temporary password ................. 403 PASSWORD_CHANGE_REQUIRED
13. Sets an own password, logs in again, sets the security question (same as steps 2-4)
14. Supplier code screen: the list of the user's own supplier codes (even if only one) -> picks one -> dashboard
15. Menu shows only what the roles (direct and through groups) allow

EVERY LATER LOGIN (every user)
16. Login -> supplier code screen -> picks one -> dashboard

LATER
17. New supplier code: tick its roles once in the role group -> every user of the group gets them
18. Forgot password on the login page -> answers the security question -> new password
```

## A3. Screens to build

| # | Screen | Who sees it | APIs |
|---|---|---|---|
| 1 | Login | everyone | `POST /api/auth/login` |
| 2 | Set new password | first login, expired password | `POST /api/auth/changepassword` |
| 3 | Set security question | first login (and profile, to change it) | `GET /api/securityquestions`, `PUT /api/securityquestions/mine` |
| 4 | Forgot password (3 steps) | login page link | `POST /api/auth/forgotpassword/start`, `/verify`, `/reset` |
| 5 | Supplier code screen (after every login, all users) + header switcher | after login | `GET /api/auth/mysuppliercodes`, `POST /api/auth/selectsuppliercode` |
| 6 | Menu / buttons | always | `GET /api/auth/mypermissions` |
| 7 | Users list | `user.view` | `GET /api/users` |
| 8 | User create / edit | `user.add` / `user.edit` | `POST /api/users`, `PUT /api/users/{id}`, `GET /api/roles`, `GET /api/rolegroups` |
| 9 | User access (role groups + roles + custom rights) | `user.view`, save needs `user.access` | `GET /api/users/{id}/access`, `PUT …/rolegroups`, `PUT …/roles`, `PUT …/rights`, `GET /api/pages` |
| 9a | Role groups: list, create / rename, roles of a group, delete | `role.view` / `role.add` / `role.edit` / `role.delete` | `/api/rolegroups…` (B8a) |
| 10 | Roles list | `role.view` | `GET /api/roles` |
| 11 | Role form | `role.add` / `role.edit` | `POST /api/roles`, `PUT /api/roles/{id}`, `GET /api/suppliercodes`, `GET /api/passwordpolicies` |
| 12 | Role rights grid | `role.view`, save needs `role.edit` | `GET /api/roles/{id}/rights`, `PUT /api/roles/{id}/rights` |
| 13 | Security settings | `securityconfig.view` / `.edit` | `GET /api/securityconfig`, `PUT /api/securityconfig/{key}` |
| 14 | Password policies | `passwordpolicy.view` / `.edit` | `GET /api/passwordpolicies`, `PUT /api/passwordpolicies/{id}` |
| 15 | My sessions (optional) | everyone | `GET /api/auth/sessions`, `DELETE /api/auth/sessions/{id}` |

## A4. Rules that matter

1. **Show / hide by permission key.** Show "Add user" only if `permissions` contains `user.add`. The server checks every call
   anyway; hiding keeps the screen clean.
2. **Never send company or supplier code yourself** (except: the picker, a role's supplier code in the role form, a custom
   right's supplier code, and Admin's `companyId` when creating a user). The server takes them from the session.
3. **Handle errors by `errorCode`**, never by message text (table in B3).
4. **Full-list saves.** Role rights, user roles and user custom rights are saved as the complete list; anything missing is removed.
5. **Rights change at once.** When an admin changes someone's roles or rights, it applies on that user's next API call (no
   new login). Re-read `mypermissions` after switching supplier code and on page refresh.
6. **The picker only shows the user's own supplier codes.** A supplier code where the user has no role or right never appears.
7. **Admin works in the company of the picked supplier code.** Roles and users lists show that company. Without a
   picked supplier code, the roles list shows only the Admin role + the default templates.
8. **Nobody changes their own roles or rights** (except Admin). Hide the Save buttons on your own access screen.

---

# PART B — API REFERENCE (request / response)

## B1. Basics

- **Base URL (dev):** `http://localhost:5180` / `https://localhost:7180`. Swagger: `/swagger`. All paths start with `/api/`.
- **JSON** is camelCase. **Dates** are Indian time (IST) without a zone, e.g. `"2026-10-08T13:19:56"`. Show as they are.
- **Every response has a body.** Success returns the object itself (no `{ success, data }` wrapper). Actions without data
  return `{ "message": "…" }`. Paged lists return `{ items, page, pageSize, totalCount }`.
- **Routes** are lowercase without `-`.

## B2. Login cookie and axios setup

Login sets two cookies:
- `jwt` — **HttpOnly** (JavaScript cannot read it); the browser sends it with every request.
- `XSRF-TOKEN` — readable; its value must go in the header `X-XSRF-TOKEN` on every POST / PUT / DELETE.

The web app never stores or sends the token itself (`accessToken` in the login body is for the desktop app and Swagger,
which use `Authorization: Bearer …`). Logout clears both cookies.

```ts
import axios from "axios";

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,   // e.g. https://localhost:7180
  withCredentials: true,                    // send the jwt cookie
  xsrfCookieName: "XSRF-TOKEN",             // axios reads this cookie ...
  xsrfHeaderName: "X-XSRF-TOKEN",           // ... and sends it on POST/PUT/DELETE
  withXSRFToken: true,                      // axios >= 1.6: also for another origin in dev
});
```

In dev, run the React app through the Vite proxy (same origin) or make sure the API's `Cors:AllowedOrigins` has its origin.

## B3. Errors

Every error has the same shape (ProblemDetails):

```json
{
  "type": "https://errors.stliquortnt.local/permission_denied",
  "status": 403,
  "errorCode": "PERMISSION_DENIED",
  "title": "You do not have the right to do this.",
  "detail": "This action needs the permission 'user.view' in the selected supplier code. Ask your administrator to add it to one of your roles.",
  "instance": "/api/users",
  "correlationId": "14610de74e6c4bbe8b1073c03441b460"
}
```

Show `title` (and `detail` where useful). Show `correlationId` on unexpected errors so support can find the log.
`400 VALIDATION_FAILED` adds `errors`, keyed by camelCase field:

```json
{ "status": 400, "errorCode": "VALIDATION_FAILED", "title": "One or more fields are invalid.",
  "errors": { "password": ["Password must be at least 8 characters.", "Password must contain an uppercase letter.", "Password must contain a number."] } }
```

**Handle these once, in an axios response interceptor:**

| errorCode | Status | What the app does |
|---|---|---|
| `UNAUTHENTICATED`, `SESSION_INVALID`, `SESSION_TIMED_OUT` | 401 | clear the user → login page |
| `SESSION_EXPIRED` | 401 | keep the screen, password popup → `POST /api/auth/login` (same user) → `POST /api/auth/selectsuppliercode` (the same supplier code as before) → **retry** the failed call |
| `SECURITY_QUESTION_REQUIRED` | 403 | open "Set security question" |
| `SUPPLIER_CODE_NOT_SELECTED` | 409 | open the supplier code picker; after picking, reload `mypermissions` and retry |
| `PERMISSION_DENIED` | 403 | toast "You don't have the right to do this" (`detail` names the right); stay on the page |
| `CSRF_REJECTED` | 403 | client bug: `X-XSRF-TOKEN` header missing — check the axios setup |
| `VALIDATION_FAILED` | 400 | show `errors[field]` under the fields |
| `NOT_FOUND` | 404 | "Not found" (also for records of another company) |
| `DATABASE_ERROR`, `UNEXPECTED_ERROR` | 500 | "Something went wrong" + `correlationId` |

Screen-specific codes are listed with each API.

---

## B4. Login and first login

### Step 1 — Login · `POST /api/auth/login` (public)

```json
// request
{ "userName": "ravi.kumar", "password": "Temp@12345" }
```

**First login (temporary password)** → `403`:
```json
{ "status": 403, "errorCode": "PASSWORD_CHANGE_REQUIRED",
  "title": "Password change required.", "detail": "Set a new password before logging in." }
```
No session and no cookie are created. Open "Set new password" with the same user name and the password just typed.

**Normal login** → `200` (`LoginResponse`), cookies set:
```json
{
  "accessToken": "eyJhbGciOiJI…",
  "expiresAt": "2026-10-09T13:19:57.0159441",
  "idleTimeoutMinutes": 60,
  "user": {
    "userId": 286, "userName": "ravi.kumar", "fullName": "Ravi Kumar", "companyId": 20,
    "forcePasswordChange": false, "passwordExpiresAt": null,
    "securityQuestionRequired": true
  },
  "supplierCodes": [
    { "id": 30, "companyId": 20, "companyName": "Globus Spirits Ltd", "franchiseName": null, "exciseId": 2,
      "exciseCode": "RJ", "supplierCode": "772", "liquorCategoryId": 2, "liquorCategoryCode": "CL",
      "displayName": "RJ CL 772", "isActive": true, "createdAt": "2026-10-07T15:15:35" }
  ],
  "activeSupplierCode": null
}
```
`activeSupplierCode` is **always `null` at login**: nothing is picked yet, even when the user has only one supplier code.

What to do after a `200`, in this order:
```
if user.securityQuestionRequired     -> "Set security question" screen (Step 3), then continue
if supplierCodes is empty            -> "No supplier code is assigned to you. Contact your administrator."
else                                 -> supplier code screen (B5) with supplierCodes -> user picks one -> dashboard
```
This happens after **every** login, for **every** user.

| Error | Status | Screen action |
|---|---|---|
| `VALIDATION_FAILED` | 400 | empty fields |
| `INVALID_CREDENTIALS` | 401 | "User name or password is incorrect." (wrong attempts are counted) |
| `USER_LOCKED` | 403 | show `detail` (contains the unlock time). Default: 3 wrong passwords in a day → locked 24 h |
| `USER_INACTIVE` / `USER_BLOCKED` | 403 | "Contact your administrator" |
| `PASSWORD_CHANGE_REQUIRED` | 403 | "Set new password" (Step 2) |
| `PASSWORD_EXPIRED` | 403 | "Your password has expired" → "Set new password" (Step 2) |
| `SESSION_LIMIT_REACHED` | 409 | "Already logged in on the maximum number of devices (default 2). Log out there first." |

### Step 2 — Set new password · `POST /api/auth/changepassword` (public)

Verified by the current (temporary) password, so it works before login.

```json
// request
{ "userName": "ravi.kumar", "currentPassword": "Temp@12345", "newPassword": "Ravi@2026Strong" }
// 200
{ "message": "Password changed. All sessions have been ended; log in with the new password." }
```

A weak password → `400`, with every broken rule of the user's password policy:
```json
{ "errorCode": "VALIDATION_FAILED",
  "errors": { "password": ["Password must be at least 8 characters.", "Password must contain an uppercase letter.", "Password must contain a number."] } }
```

Rules come from the password policy of the user's roles (several roles → the strictest). The new password must also differ
from the current one, must not contain the user name, and must not be one of the last N passwords. Wrong `currentPassword`
→ `401 INVALID_CREDENTIALS` (counts as a wrong attempt).

**After 200:** log in again automatically with the new password (Step 1) — the user does not have to type it again.

### Step 3 — Set security question (first login)

Load the list (public): `GET /api/securityquestions` — 10 questions, show them in a dropdown
```json
[
  { "id": 1,  "questionText": "What is the name of your first school?" },
  { "id": 2,  "questionText": "What is your mother's maiden name?" },
  { "id": 3,  "questionText": "What was the name of your first pet?" },
  { "id": 4,  "questionText": "In which city were you born?" },
  { "id": 5,  "questionText": "What is your favourite book?" },
  { "id": 6,  "questionText": "What was the name of your childhood best friend?" },
  { "id": 7,  "questionText": "What was the model of your first vehicle?" },
  { "id": 8,  "questionText": "What is the name of the street you grew up on?" },
  { "id": 9,  "questionText": "What was the name of the company where you had your first job?" },
  { "id": 10, "questionText": "What is the name of your favourite teacher?" }
]
```
Questions are fixed by us (no screen edits them). A question someone has answered never changes its text and is never
deleted, so a saved answer always matches the question shown later in "forgot password". A question may be retired: it
disappears from this list, but users who chose it keep it.

Save (logged in): `PUT /api/securityquestions/mine`
```json
// request — the password again, so an unattended screen cannot change it (the app may reuse the one from Step 2)
{ "questionId": 1, "answer": "Jaipur", "currentPassword": "Ravi@2026Strong" }
// 200
{ "message": "Security question saved: \"What is the name of your first school?\"" }
```

Until this is saved, every other API (except `GET /api/auth/me` and logout) answers:
```json
{ "status": 403, "errorCode": "SECURITY_QUESTION_REQUIRED", "title": "Security question not set.",
  "detail": "Choose a security question and answer (PUT /api/securityquestions/mine) before continuing." }
```

- One question per user. The answer needs at least 2 characters (spaces do not count).
- **How the answer is matched later:** upper/lower case and **all spaces are ignored** — `New Delhi` = `newdelhi` =
  `  NEW  DELHI `. Spelling and punctuation must match — `Jaipur` ≠ `Jaypur`, `St. Mary's` ≠ `St Marys`. A hint under the
  answer box helps: "Spaces and capital letters do not matter; spelling and punctuation do."
- The same `PUT` changes the question later (profile screen).
- Users who already have a question, or when security questions are switched off (`SECURITY_QUESTION_ENABLED = 0`), never see this step.
- Errors: `401 INVALID_CREDENTIALS` (wrong password), `404 NOT_FOUND` (question id), `400` (answer too short).

### Page refresh / app start

```
GET /api/auth/me              -> 401 = not logged in -> login page
                                 securityQuestionRequired = true -> "Set security question"
GET /api/auth/mypermissions   -> activeSupplierCode == null -> supplier code screen (GET /api/auth/mysuppliercodes)
                                 else -> stay on the current screen (the session still has its supplier code)
```

`GET /api/auth/me`
```json
{ "userId": 1, "userName": "admin", "fullName": "Administrator", "companyId": null,
  "forcePasswordChange": false, "passwordExpiresAt": null, "securityQuestionRequired": false }
```

### Logout · `POST /api/auth/logout`
```json
{ "message": "Logged out." }
```
Clears the cookies. Go to the login page.

### My sessions (optional) · `GET /api/auth/sessions`
```json
[ { "id": 62, "loginAt": "2026-10-08T13:19:57", "lastActivityAt": "2026-10-08T13:19:57", "expiresAt": "2026-10-08T14:19:57",
    "ipAddress": "::1", "userAgent": "Mozilla/5.0 …", "isCurrent": true } ]
```
`DELETE /api/auth/sessions/{id}` logs out another device → `MessageResponse`.

### Session timing
- **Idle:** no call for `SESSION_IDLE_MINUTES` (default 60) → `401 SESSION_TIMED_OUT` → login page.
- **Hard limit:** `SESSION_EXPIRY_MINUTES` after login (default 24 h, = `expiresAt`) → `401 SESSION_EXPIRED` → password popup,
  `POST /api/auth/login`, then **`POST /api/auth/selectsuppliercode` with the supplier code the user was working in** (the new
  session starts without one), then retry the failed call. The user stays on the same screen.

---

## B5. Supplier code screen and menu

**After every login, every user lands on this screen** — a full page listing the user's supplier codes (from the login
response's `supplierCodes`, or `GET /api/auth/mysuppliercodes`). Even a user with one supplier code sees it and picks it.
Picking one calls `selectsuppliercode` and opens the dashboard. The header keeps a "switch" dropdown with the same list.

`GET /api/auth/mysuppliercodes` → only the user's supplier codes (Admin: every active one of every company)
```json
[
  { "id": 32, "companyId": 20, "companyName": "Globus Spirits Ltd", "exciseCode": "JK", "supplierCode": "369", "liquorCategoryCode": "IMFL", "displayName": "JK IMFL 369", "…": "…" },
  { "id": 30, "companyId": 20, "companyName": "Globus Spirits Ltd", "exciseCode": "RJ", "supplierCode": "772", "liquorCategoryCode": "CL", "displayName": "RJ CL 772", "…": "…" },
  { "id": 31, "companyId": 20, "companyName": "Globus Spirits Ltd", "exciseCode": "RJ", "supplierCode": "1028", "liquorCategoryCode": "IMFL", "displayName": "RJ IMFL 1028", "…": "…" }
]
```
For Admin, group the picker by `companyName`.

`POST /api/auth/selectsuppliercode` → picks or switches; returns the permissions directly
```json
// request
{ "supplierCodeId": 30 }
// 200 (MyPermissionsResponse)
{
  "isSuperAdmin": false,
  "activeSupplierCode": { "id": 30, "companyName": "Globus Spirits Ltd", "displayName": "RJ CL 772", "…": "…" },
  "permissions": ["liquorcategory.view", "suppliercode.view"]
}
```
Not the user's supplier code → `403 SUPPLIER_CODE_NOT_ASSIGNED`.

`GET /api/auth/mypermissions` → same shape. `permissions` is empty while no supplier code is picked (Admin always has
all keys). Build the menu and buttons from it; show `activeSupplierCode.displayName` in the header with a "switch" dropdown.

---

## B6. Users

### List · `GET /api/users?search=ravi&page=1&pageSize=50` — `user.view`
Users of the session's company. `search` matches user name / full name; `pageSize` max 200.
```json
{
  "items": [
    { "id": 286, "userName": "ravi.kumar", "fullName": "Ravi Kumar", "email": "ravi.kumar@globus-spirits.example",
      "phone": "9000000201", "employeeCode": "GSL-201", "companyId": 20, "isActive": true, "isBlocked": false,
      "failedLoginAttempts": 0, "lockedUntil": null, "forcePasswordChange": false, "passwordExpiresAt": null,
      "lastLoginAt": "2026-10-08T13:19:57", "createdAt": "2026-10-08T13:19:56" }
  ],
  "page": 1, "pageSize": 50, "totalCount": 1
}
```
Show a "Locked" badge when `lockedUntil` is in the future, "Inactive" when `isActive` is false, "Must change password"
when `forcePasswordChange` is true. `GET /api/users/{id}` returns one `UserResponse` (same shape as an item).

### Create · `POST /api/users` — `user.add`

Form: user name, temporary password, full name, email, phone, employee code, "must change password at first login"
(default on), **role groups** and/or **roles** (at least one of the two). Role groups come from `GET /api/rolegroups` (B8a) —
for most users one group is enough ("All Operators"). Roles come from `GET /api/roles` (B8) shown by `displayName`; easiest UI:
pick a supplier code, then tick its roles from `GET /api/roles?supplierCodeId=…`. Company-level roles are in every list.

```json
// request
{
  "userName": "ravi.kumar",
  "password": "Temp@12345",
  "companyId": 20,
  "roles": [ { "roleId": 98 } ],
  "roleGroupIds": [],
  "fullName": "Ravi Kumar",
  "email": "ravi.kumar@globus-spirits.example",
  "phone": "9000000201",
  "employeeCode": "GSL-201",
  "forcePasswordChange": true
}
// or with a role group only:  "roles": [], "roleGroupIds": [2]
// 201 (UserResponse)
{ "id": 286, "userName": "ravi.kumar", "fullName": "Ravi Kumar", "email": "ravi.kumar@globus-spirits.example",
  "phone": "9000000201", "employeeCode": "GSL-201", "companyId": 20, "isActive": true, "isBlocked": false,
  "failedLoginAttempts": 0, "lockedUntil": null, "forcePasswordChange": true, "passwordExpiresAt": null,
  "lastLoginAt": null, "createdAt": "2026-10-08T13:19:56.2769256" }
```

- `roles` / `roleGroupIds`: at least one role **or** one group. For a role send only `roleId` (the supplier code comes with the
  role; 98 = "Operator RJ CL 772"). The password policy is the strictest of all roles, direct and inside the groups.
- `companyId`: **only Admin** sends it; when left out it is the company of the picked supplier code. Everyone else's
  users always join their own company.
- `userName`: letters, digits and `. _ @ -`, max 50, cannot be changed later.
- The temporary password must already satisfy the password policy of the chosen roles:
  ```json
  { "errorCode": "VALIDATION_FAILED",
    "errors": { "password": ["Password must be at least 8 characters.", "Password must contain an uppercase letter.", "Password must contain a number."] } }
  ```

### Edit profile · `PUT /api/users/{id}` — `user.edit`
Profile fields only (send all four; roles and status have their own APIs):
```json
// request
{ "fullName": "Ravi Kumar Sharma", "email": "ravi.kumar@globus-spirits.example", "phone": "9000000201", "employeeCode": "GSL-201" }
// 200 → UserResponse
```

### Activate / deactivate / unlock — `user.status` / `user.unlock`
`POST /api/users/{id}/deactivate`, `POST /api/users/{id}/activate`, `POST /api/users/{id}/unlock` → `UserResponse`
(deactivating also ends the user's sessions; unlock clears `lockedUntil`, the wrong-attempt count and an `isBlocked` block).

| Error | Status | Meaning |
|---|---|---|
| `USERNAME_TAKEN` | 409 | user name exists |
| `PASSWORD_POLICY_NOT_CONFIGURED` | 409 | a chosen role has no password policy |
| `VALIDATION_FAILED` | 400 | `errors.password` (policy rules), `errors.roles` (no role and no group), other fields |
| `ADMIN_USER_PROTECTED` | 403 | target is an admin user (Plant Admin / Admin), or an admin role / a group with an admin role is chosen, and the caller lacks `user.manageadmin` |
| `CANNOT_DEACTIVATE_SELF` | 409 | deactivating yourself |
| `NOT_FOUND` | 404 | user / role / role group of another company, role of a deactivated supplier code, unknown company |

---

## B7. User access: role groups, roles and custom rights

A user's rights = rights of the **roles given directly** + rights of the **roles inside the user's role groups** + **custom
rights**. Groups are optional; a user needs at least one role or one group.

### Load · `GET /api/users/{id}/access` — `user.view`
```json
{
  "userId": 392, "userName": "zz.group.demo",
  "roles": [],
  "roleGroups": [
    { "roleGroupId": 2, "groupName": "All Operators",
      "roles": [
        { "roleId": 98, "roleName": "Operator", "displayName": "Operator RJ CL 772",   "supplierCodeId": 30, "supplierCodeName": "RJ CL 772" },
        { "roleId": 97, "roleName": "Operator", "displayName": "Operator RJ IMFL 1028", "supplierCodeId": 31, "supplierCodeName": "RJ IMFL 1028" }
      ] }
  ],
  "rights": []
}
```
`roles` holds only the directly given roles; the group's roles are listed inside `roleGroups`. `supplierCodeName` is
`"All supplier codes"` for a company-level role or an all-supplier-codes right.

### Save role groups · `PUT /api/users/{id}/rolegroups` — `user.access` (the FULL list)
```json
// request
{ "roleGroupIds": [2] }
// 200 → UserAccessResponse (as above)
```
```json
// removing the last group of a user who has no direct role → 400
{ "errorCode": "VALIDATION_FAILED", "errors": { "roles": ["Give the user at least one role or one role group."] } }
```

### Save roles · `PUT /api/users/{id}/roles` — `user.access` (the FULL list; may be empty while the user holds a group)
```json
// request
{ "roles": [ { "roleId": 98 }, { "roleId": 133 } ] }
// 200 (UserAccessResponse)
{
  "userId": 286, "userName": "ravi.kumar",
  "roles": [
    { "roleId": 98,  "roleName": "Operator",         "displayName": "Operator RJ CL 772",         "supplierCodeId": 30, "supplierCodeName": "RJ CL 772" },
    { "roleId": 133, "roleName": "Packing Operator", "displayName": "Packing Operator RJ CL 772", "supplierCodeId": 30, "supplierCodeName": "RJ CL 772" }
  ],
  "rights": []
}
```

### Save custom rights · `PUT /api/users/{id}/rights` — `user.access` (the FULL list, may be empty)
Each right is a page action (from `GET /api/pages`, B9 — show the application next to the page name) for one supplier code,
or `supplierCodeId: null` = all supplier codes. This list always holds the user's custom rights of both applications.
```json
// request
{ "rights": [ { "pageActionId": 5, "supplierCodeId": 30 } ] }
// 200
{
  "userId": 286, "userName": "ravi.kumar",
  "roles": [ "…" ],
  "rights": [ { "pageActionId": 5, "permissionKey": "user.unlock", "actionName": "Unlock", "supplierCodeId": 30, "supplierCodeName": "RJ CL 772" } ]
}
```

**Screen:** three sections. "Role groups": the user's groups (chips; each expandable to show its roles), add / remove from
`GET /api/rolegroups`, Save. "Roles (direct)": rows of `displayName` grouped by `supplierCodeName`, add / remove, Save. "Custom rights":
rows *Right* (`pageName → actionName`) × *Supplier code* (or "All"), add / remove, Save. In the right picker disable
`grantScope = "SYSTEM"` actions, and `"ADMIN"` actions unless the user has `user.manageadmin`. Hide both Save buttons when
looking at yourself (unless Admin).

| Error | Status | Meaning |
|---|---|---|
| `CANNOT_CHANGE_OWN_ACCESS` | 409 | editing your own roles / groups / rights |
| `ADMIN_USER_PROTECTED` | 403 | admin user / admin role / group with an admin role / ADMIN right without `user.manageadmin` |
| `RIGHT_NOT_GRANTABLE` | 403 | a SYSTEM right in custom rights |
| `PASSWORD_POLICY_NOT_CONFIGURED` | 409 | a role without password policy |
| `VALIDATION_FAILED` | 400 | `errors.roles` (no role and no group left), `errors.rights` (unknown action id) |
| `NOT_FOUND` | 404 | user / role / role group / supplier code not of this company |

---

## B8. Master roles

### How roles are organised
```
Company-level (all supplier codes):  Plant Admin · Agent Manager
RJ CL 772:      Plant Manager RJ CL 772 · Supervisor RJ CL 772 · Operator RJ CL 772 · Viewer RJ CL 772
RJ IMFL 1028:   Plant Manager RJ IMFL 1028 · Supervisor … · Operator … · Viewer …
JK IMFL 369:    Plant Manager JK IMFL 369 · Supervisor … · Operator … · Viewer …
```
When Admin adds a supplier code, its 4 default roles are created automatically with default rights (and a new
company also gets Plant Admin and Agent Manager). The company may then edit, add or delete roles.

Default rights:

| Role | Default rights |
|---|---|
| Plant Admin (admin role) | everything except the Admin-only (SYSTEM) rights: users (incl. `user.manageadmin`), roles, security settings, password policies, supplier code / liquor category view |
| Agent Manager | users (not admin users), roles, supplier code / liquor category view |
| Plant Manager | `user.view`, `role.view`, `suppliercode.view`, `liquorcategory.view` |
| Supervisor / Operator / Viewer | `suppliercode.view`, `liquorcategory.view` (production pages add theirs later) |

### List · `GET /api/roles` — `role.view`
Company-level first, then grouped by supplier code. `GET /api/roles?supplierCodeId=30` → only roles usable in RJ CL 772
(its own + company-level).
```json
[
  { "id": 68, "companyId": 20, "supplierCodeId": null, "supplierCodeName": null, "roleName": "Agent Manager", "displayName": "Agent Manager",
    "description": "Creates users, roles and rights (not admin users)", "isSystem": false, "isTemplate": false,
    "perSupplierCode": false, "isAdminRole": false, "isActive": true, "passwordPolicyId": 2 },
  { "id": 67, "companyId": 20, "supplierCodeId": null, "supplierCodeName": null, "roleName": "Plant Admin", "displayName": "Plant Admin",
    "description": "Head of the plant: every right of the company", "isSystem": false, "isTemplate": false,
    "perSupplierCode": false, "isAdminRole": true, "isActive": true, "passwordPolicyId": 2 },
  { "id": 98, "companyId": 20, "supplierCodeId": 30, "supplierCodeName": "RJ CL 772", "roleName": "Operator", "displayName": "Operator RJ CL 772",
    "description": "Works on the line (rights added with each page)", "isSystem": false, "isTemplate": false,
    "perSupplierCode": false, "isAdminRole": false, "isActive": true, "passwordPolicyId": 2 },
  "…"
]
```
**Always show `displayName`.** Badges: `isAdminRole` → "Company admin", `isSystem` → "System" (the Admin role; read-only, no Edit /
Rights / Delete). Admin with no supplier code picked sees the Admin role + the default templates (`isTemplate`).

`GET /api/roles/{id}` → one `RoleResponse`.

### Create · `POST /api/roles` — `role.add`
Form: name, **supplier code** (dropdown from `GET /api/suppliercodes` + "Company-level (all supplier codes)" = `null`),
description, admin role checkbox (only if `user.manageadmin`, only company-level), password policy (dropdown from
`GET /api/passwordpolicies`).
```json
// request
{ "roleName": "Packer", "supplierCodeId": 30, "description": "Packing line", "isAdminRole": false, "passwordPolicyId": 2 }
// 201 (RoleResponse)
{ "id": 133, "companyId": 20, "supplierCodeId": 30, "supplierCodeName": "RJ CL 772", "roleName": "Packer",
  "displayName": "Packer RJ CL 772", "description": "Packing line", "isSystem": false, "isTemplate": false,
  "perSupplierCode": false, "isAdminRole": false, "isActive": true, "passwordPolicyId": 2 }
```
A new role has no rights yet; open the rights grid next. The same name can exist once per supplier code.
```json
// same name again for RJ CL 772 → 409
{ "errorCode": "ROLE_NAME_TAKEN", "title": "A role with this name already exists.",
  "detail": "This supplier code already has a role 'Packer'. Choose another name." }
```

### Edit · `PUT /api/roles/{id}` — `role.edit`
Same body. **The supplier code cannot change** — show it read-only and send the same value.
```json
// request
{ "roleName": "Packing Operator", "supplierCodeId": 30, "description": "Packing line, shift A", "isAdminRole": false, "passwordPolicyId": 2 }
// 200
{ "id": 133, "supplierCodeId": 30, "supplierCodeName": "RJ CL 772", "roleName": "Packing Operator",
  "displayName": "Packing Operator RJ CL 772", "description": "Packing line, shift A", "…": "…" }
```
```json
// another supplierCodeId → 400
{ "errorCode": "VALIDATION_FAILED",
  "errors": { "supplierCodeId": ["The supplier code of a role cannot be changed. Create a new role for the other supplier code."] } }
```

### Delete · `DELETE /api/roles/{id}` — `role.delete`
```json
{ "message": "Role 'Packing Operator RJ CL 772' deleted." }
```
Refused with `409 ROLE_IN_USE` while any user holds the role — remove it from those users first.

| Error | Status | Meaning |
|---|---|---|
| `ROLE_NAME_TAKEN` | 409 | name already used for this supplier code (or among company-level roles) |
| `ROLE_IN_USE` | 409 | delete refused: users hold the role |
| `ROLE_NOT_EDITABLE` | 409 | Admin role, or a template edited by a company user |
| `ADMIN_USER_PROTECTED` | 403 | admin role or ADMIN right without `user.manageadmin` |
| `VALIDATION_FAILED` | 400 | `errors.supplierCodeId` (changed), `errors.isAdminRole` (admin role with a supplier code), `errors.roleName` |
| `NOT_FOUND` | 404 | role of another company, supplier code not of the company, unknown password policy |

---

## B8a. Role groups

A **role group** is a named bundle of master roles of the company, given to users as one piece — e.g. "All Operators" =
Operator RJ CL 772 + Operator RJ IMFL 1028. Why: when a new supplier code comes, its "Operator" role is ticked **once** in
the group and all 50 operators get it, instead of giving the role to 50 users one by one. Groups are optional; roles can
still be given directly, or both.

Rules:
- A group holds roles of its own company only (no rights, no other groups). Any role may go in, also company-level ones.
- A new supplier code's roles are **not** added to groups automatically — the admin ticks them in the group.
- Changing a group's roles changes the rights of every user of the group on their next call.
- A group given to any user cannot be deleted (`409 ROLE_GROUP_IN_USE`); a role inside a group cannot be deleted (`409 ROLE_IN_USE`).
- A group with an admin role (Plant Admin), or held by an admin user, is changed or given only with `user.manageadmin`.
- Nobody (except Admin) changes a group they hold themselves.
- Group screens use the role rights: `role.view` / `role.add` / `role.edit` / `role.delete`. Giving a group to a user: `user.access`.

| Call | Right | Request → Response |
|---|---|---|
| `GET /api/rolegroups` | `role.view` | → `RoleGroupResponse[]` (the company's groups, by name) |
| `GET /api/rolegroups/{id}` | `role.view` | → `RoleGroupResponse` |
| `POST /api/rolegroups` | `role.add` | `{ groupName, description }` → `201 RoleGroupResponse` (no roles yet) |
| `PUT /api/rolegroups/{id}` | `role.edit` | `{ groupName, description }` → `RoleGroupResponse` |
| `PUT /api/rolegroups/{id}/roles` | `role.edit` | `{ roleIds: [] }` (the FULL list) → `RoleGroupResponse` |
| `DELETE /api/rolegroups/{id}` | `role.delete` | → `MessageResponse` |

```json
// POST /api/rolegroups
{ "groupName": "All Operators", "description": "Operators of every supplier code" }
// 201
{ "id": 2, "companyId": 20, "groupName": "All Operators", "description": "Operators of every supplier code",
  "isActive": true, "hasAdminRole": false, "userCount": 0, "roles": [] }

// PUT /api/rolegroups/2/roles
{ "roleIds": [98, 97] }
// 200
{ "id": 2, "companyId": 20, "groupName": "All Operators", "description": "Operators of every supplier code",
  "isActive": true, "hasAdminRole": false, "userCount": 0,
  "roles": [
    { "roleId": 98, "roleName": "Operator", "displayName": "Operator RJ CL 772",    "supplierCodeId": 30, "supplierCodeName": "RJ CL 772",    "isAdminRole": false },
    { "roleId": 97, "roleName": "Operator", "displayName": "Operator RJ IMFL 1028", "supplierCodeId": 31, "supplierCodeName": "RJ IMFL 1028", "isAdminRole": false }
  ] }

// new supplier code JK 369: tick its Operator once → every user of the group gets it
{ "roleIds": [98, 97, 96] }      // 200, "userCount": 1 — the response shows how many users are affected

// DELETE /api/rolegroups/2 while a user holds it → 409
{ "errorCode": "ROLE_GROUP_IN_USE", "title": "This role group is still given to users.",
  "detail": "Remove 'All Operators' from every user (1) before deleting it." }
// after removing it from the users → 200
{ "message": "Role group 'All Operators' deleted." }
```

**Screens:** a "Role groups" list (name, description, number of roles, `userCount`, "Admin" badge when `hasAdminRole`),
a create / rename form, and a "roles of the group" screen: the company's roles from `GET /api/roles` grouped by
`supplierCodeName`, with checkboxes and one Save (show "Changes the access of N users" with `userCount` before saving).

| Error | Status | Meaning |
|---|---|---|
| `ROLE_GROUP_NAME_TAKEN` | 409 | name already used in this company |
| `ROLE_GROUP_IN_USE` | 409 | delete refused: users hold the group |
| `CANNOT_CHANGE_OWN_ACCESS` | 409 | changing a group you hold yourself |
| `ADMIN_USER_PROTECTED` | 403 | group with an admin role, or held by an admin user, without `user.manageadmin` |
| `PASSWORD_POLICY_NOT_CONFIGURED` | 409 | a ticked role has no password policy |
| `VALIDATION_FAILED` | 400 | empty name; creating a group before picking a supplier code (Admin) |
| `NOT_FOUND` | 404 | group or role of another company, template / inactive role |

---

## B9. Pages and the rights grid

### Two applications

Roles and rights are shared by **two applications**: the **web application** (this one, on the server) and the **line
application** (desktop, at the production line). Every page belongs to exactly one of them — `applicationType` is
`"WEB"` or `"LINE"`. One role can hold rights of both (e.g. "Operator RJ CL 772" gets line-application rights). A job done
in both apps has two pages with their own keys (e.g. `batch` WEB, `linebatch` LINE). Today every page is `WEB`; line pages
will be added by us when the line application is built.

### Pages and their actions (seeded by us)

`GET /api/pages` — `role.view`. Rows of the grid; each page has its **own** actions (not a fixed View/Add/Edit/Delete set).
`GET /api/pages?applicationType=WEB` (or `LINE`) → only that application's pages. Web pages come first.

| Page (module) — all `WEB` today | Actions → permission key (id) |
|---|---|
| User (Administration) | View `user.view` (1) · Create `user.add` (2) · Edit `user.edit` (3) · Activate / deactivate `user.status` (4) · Unlock `user.unlock` (5) · Assign roles and rights `user.access` (6) · Manage admin users `user.manageadmin` (7, ADMIN) |
| Role (Administration) | View `role.view` (8) · Create `role.add` (9) · Edit (name and rights) `role.edit` (10) · Delete `role.delete` (11) |
| Security Config (Administration) | View `securityconfig.view` (12, ADMIN) · Edit `securityconfig.edit` (13, ADMIN) |
| Password Policy (Administration) | View `passwordpolicy.view` (14, ADMIN) · Edit `passwordpolicy.edit` (15, ADMIN) |
| Supplier Code (Masters) | View `suppliercode.view` (16) · Create (17, SYSTEM) · Edit / activate (18, SYSTEM) |
| Liquor Category (Masters) | View `liquorcategory.view` (19) · Create (20, SYSTEM) · Edit / activate (21, SYSTEM) |
| Company (Masters) | View `company.view` (32, SYSTEM) |

Use the ids from the API, not from this table. A page without actions (e.g. "Brand", not built yet) may appear — skip it.
New pages (batch, plan …) will simply add rows; the grid code does not change.

**`grantScope`:** `ANY` = anyone with `role.edit` may tick it · `ADMIN` = only with `user.manageadmin` · `SYSTEM` = Admin
only, never tickable (show disabled).

```json
[
  { "pageId": 1, "pageKey": "user", "pageName": "User", "moduleName": "Administration", "applicationType": "WEB",
    "actions": [
      { "pageActionId": 1, "actionKey": "view", "permissionKey": "user.view", "actionName": "View", "grantScope": "ANY", "granted": false },
      { "pageActionId": 2, "actionKey": "add", "permissionKey": "user.add", "actionName": "Create", "grantScope": "ANY", "granted": false },
      "…",
      { "pageActionId": 7, "actionKey": "manageadmin", "permissionKey": "user.manageadmin", "actionName": "Manage admin users", "grantScope": "ADMIN", "granted": false }
    ] },
  "…"
]
```

### A role's grid · `GET /api/roles/{id}/rights` — `role.view`
Same pages, with `granted` ticked. `?applicationType=WEB` or `LINE` → only that application's pages.
```json
{ "roleId": 98, "roleName": "Operator", "displayName": "Operator RJ CL 772",
  "pages": [ { "pageId": 1, "pageKey": "user", "pageName": "User", "moduleName": "Administration", "applicationType": "WEB",
               "actions": [ { "pageActionId": 1, "permissionKey": "user.view", "actionName": "View", "grantScope": "ANY", "granted": false }, "…" ] }, "…" ] }
```

### Save · `PUT /api/roles/{id}/rights` — `role.edit` (ids of ALL ticked actions)
```json
// request — both applications in one grid: ALL ticked ids of both
{ "pageActionIds": [16, 19, 1] }

// request — one application's tab: ALL ticked ids of THAT application; the other application's rights are kept
{ "pageActionIds": [16, 19, 1], "applicationType": "WEB" }

// 200 → the grid again (RoleRightsResponse) with these ticked (only that application when applicationType was sent)
```
**Important:** if the screen shows only one application's pages, always send `applicationType` with the save. Without it the
list is taken as the full list of both applications, and the other application's rights would be removed. An id of the
other application together with `applicationType` → `400` (`errors.pageActionIds`); an unknown `applicationType` → `400`
(`errors.applicationType`).
```json
// a SYSTEM action → 403
{ "errorCode": "RIGHT_NOT_GRANTABLE", "title": "These rights cannot be given to a role.", "detail": "suppliercode.add belong to the Admin role only." }
```

**Grid screen:** two tabs, **Web application** and **Line application** (load each with `?applicationType=…`, save each with
`applicationType`). In a tab: rows = pages grouped by `moduleName`, cells = that page's actions as checkboxes, "select
all" per row, one Save. Hide the Line tab while it has no pages. Users holding the role get the change on their next call.

---

## B10. Settings

### Security settings · `GET /api/securityconfig` — `securityconfig.view`
All rows of the `SECURITY_CONFIG` table:
```json
[
  { "key": "FAILED_LOGIN_LOCK_ENABLED", "value": "1", "dataType": "BOOL", "description": "Master switch for failed-login account locking", "updatedAt": "2026-09-28T11:09:34" },
  { "key": "MAX_FAILED_LOGIN_ATTEMPTS", "value": "3", "dataType": "INT", "description": "Consecutive wrong passwords (per day) before lock", "updatedAt": "2026-09-28T11:09:34" },
  "…"
]
```

| Key | Type | Now | Meaning | Allowed |
|---|---|---|---|---|
| `FAILED_LOGIN_LOCK_ENABLED` | BOOL | 1 | lock accounts after wrong passwords | 1 / 0 |
| `MAX_FAILED_LOGIN_ATTEMPTS` | INT | 3 | wrong passwords in one day before the lock | ≥ 1 |
| `ACCOUNT_LOCK_DURATION_MINUTES` | INT | 1440 | how long the lock lasts (1440 = 24 h) | ≥ 1 |
| `SESSION_LIMIT_ENABLED` | BOOL | 1 | limit logins at the same time | 1 / 0 |
| `MAX_ACTIVE_SESSIONS` | INT | 2 | devices logged in at once per user | ≥ 1 |
| `SESSION_FULL_BEHAVIOUR` | STRING | REJECT | what happens when the limit is reached | `REJECT` only |
| `SESSION_IDLE_MINUTES` | INT | 60 | minutes without any call before the session ends | ≥ 1 |
| `SESSION_EXPIRY_MINUTES` | INT | 1440 | hard limit of a session, even while working | ≥ 1 |
| `SECURITY_QUESTION_ENABLED` | BOOL | 1 | security questions on (first-login step + forgot password) | 1 / 0 |
| `SECURITY_QUESTION_REQUIRED` | INT | 1 | how many questions a user sets — **only 1 is supported now**; show read-only | 1 |
| `PASSWORD_RESET_EXPIRY_MINUTES` | INT | 15 | how long a forgot-password request is valid | ≥ 1 |
| `PASSWORD_RESET_MAX_ATTEMPTS` | INT | 5 | wrong answers allowed per forgot-password request | ≥ 1 |
| `LOG_MODE` | STRING | DETAIL | application log detail | `NORMAL` / `DETAIL` |

**Screen:** one form; the input type follows `dataType` (BOOL → switch, INT → number, STRING with allowed values → select).
Save each changed row:

`PUT /api/securityconfig/{key}` — `securityconfig.edit`
```json
// request (value is always a string)
{ "value": "60" }
// 200
{ "key": "SESSION_IDLE_MINUTES", "value": "60", "dataType": "INT",
  "description": "Minutes without any API call before a web session ends (sliding window)", "updatedAt": "2026-10-08T13:19:58.587563" }
```
```json
// bad value → 400
{ "errorCode": "VALIDATION_FAILED", "errors": { "value": ["Must be a whole number of at least 1."] } }
```
BOOL accepts `1`/`0`/`true`/`false`. Unknown key → `404`. Changes apply on the next request; nothing restarts.

### Password policies · `GET /api/passwordpolicies` — `passwordpolicy.view`
Used by the role form (dropdown) and its own settings screen.
```json
[
  { "id": 1, "policyName": "EASY", "minLength": 1, "maxLength": 20, "requireUppercase": false, "requireLowercase": false,
    "requireNumber": true, "requireSpecialCharacter": false, "passwordHistoryCount": 3, "passwordExpiryEnabled": false,
    "passwordExpiryDays": null, "allowUsernameInPassword": false, "allowCommonPassword": false, "status": true, "updatedAt": "2026-09-30T17:32:21" },
  { "id": 2, "policyName": "MEDIUM", "minLength": 8, "maxLength": 30, "requireUppercase": true, "requireLowercase": true,
    "requireNumber": true, "requireSpecialCharacter": false, "passwordHistoryCount": 5, "passwordExpiryEnabled": false, "passwordExpiryDays": null, "…": "…" },
  { "id": 3, "policyName": "HARD", "minLength": 12, "maxLength": 64, "requireUppercase": true, "requireLowercase": true,
    "requireNumber": true, "requireSpecialCharacter": true, "passwordHistoryCount": 5, "passwordExpiryEnabled": true, "passwordExpiryDays": 90, "…": "…" }
]
```

`PUT /api/passwordpolicies/{id}` — `passwordpolicy.edit` (the name cannot change)
```json
// request
{ "minLength": 8, "maxLength": 30, "requireUppercase": true, "requireLowercase": true, "requireNumber": true,
  "requireSpecialCharacter": false, "passwordHistoryCount": 5, "passwordExpiryEnabled": false, "passwordExpiryDays": null,
  "allowUsernameInPassword": false, "allowCommonPassword": false }
// 200 → PasswordPolicyResponse
```
Rules: lengths 1–128 and max ≥ min; history 0–50; expiry days ≥ 1 when expiry is on. A change applies to the next password set.

---

## B11. Forgot password (login page, all public)

```
1. POST /api/auth/forgotpassword/start   { userName }                     -> question + requestToken
2. POST /api/auth/forgotpassword/verify  { requestToken, answer }         -> ok
3. POST /api/auth/forgotpassword/reset   { requestToken, newPassword }    -> done, log in
```

```json
// 1. request
{ "userName": "ravi.kumar" }
// 1. 200
{ "requestToken": "vcwRKOOhuVLLM4zii2ccGnyf-0pSg_Sz1WiDYDOvk2E", "questionText": "What is the name of your first school?", "expiresAt": "2026-10-08T13:34:58.0216149" }

// 2. request (case and spaces are ignored: "jaipur", "JAI PUR" and "Jaipur" all match)
{ "requestToken": "vcwRKOOhuVLLM4zii2ccGnyf-0pSg_Sz1WiDYDOvk2E", "answer": "jaipur" }
// 2. 200
{ "message": "Answer verified. Set a new password before 2026-10-08 13:34." }
// 2. wrong answer → 401
{ "errorCode": "SECURITY_ANSWER_INCORRECT", "title": "The answer is incorrect." }

// 3. request
{ "requestToken": "vcwRKOOhuVLLM4zii2ccGnyf-0pSg_Sz1WiDYDOvk2E", "newPassword": "Ravi@2027Strong" }
// 3. 200
{ "message": "Password has been reset. Log in with the new password." }
```

Keep `requestToken` in memory between the three steps. The new password follows the same policy rules as in B4 Step 2.

| Error | Status | Meaning |
|---|---|---|
| `SECURITY_QUESTION_NOT_SET` | 409 | the user never set a question → "Ask your administrator to reset your password" |
| `SECURITY_QUESTION_DISABLED` | 409 | security questions switched off |
| `SECURITY_ANSWER_INCORRECT` | 401 | wrong answer (attempts are counted) |
| `RESET_ATTEMPTS_EXCEEDED` | 403 | too many wrong answers (default 5) → start again later |
| `RESET_REQUEST_EXPIRED` / `RESET_REQUEST_INVALID` | 409 | took too long (default 15 min) or bad token → start again |

---

## B12. Lists for dropdowns

| Call | Right | Returns |
|---|---|---|
| `GET /api/suppliercodes?search=&page=1&pageSize=50` | `suppliercode.view` | paged `SupplierCodeResponse` of the company (Admin: all) |
| `GET /api/liquorcategories` | `liquorcategory.view` | `[{ id, categoryCode: "CL", categoryName: "Country Liquor", description, isActive }]` |
| `GET /api/excises` | `suppliercode.view` | `[{ id, exciseCode: "RJ", exciseName: "Rajasthan", isActive }]` |
| `GET /api/companies` | `company.view` (Admin) | `[{ id: 20, companyName: "Globus Spirits Ltd", aliasName, city, isActive, supplierCodeCount: 3 }]` |

---

## B13. TypeScript types

```ts
// ---------- common ----------
export interface MessageResponse { message: string; }
export interface PagedResponse<T> { items: T[]; page: number; pageSize: number; totalCount: number; }
export interface ProblemDetails {
  type: string; status: number; errorCode: string; title: string; detail?: string | null;
  instance: string; correlationId: string; errors?: Record<string, string[]>;
}

// ---------- auth ----------
export interface LoginRequest { userName: string; password: string; }
export interface CurrentUserResponse {
  userId: number; userName: string; fullName?: string | null; companyId?: number | null;
  forcePasswordChange: boolean; passwordExpiresAt?: string | null;
  securityQuestionRequired: boolean;   // true → "Set security question" before anything else
}
export interface LoginResponse {
  accessToken: string;                 // ignore in the web app (cookie)
  expiresAt: string;                   // hard limit of the session (IST)
  idleTimeoutMinutes: number;
  user: CurrentUserResponse;
  supplierCodes: SupplierCodeResponse[];
  activeSupplierCode: SupplierCodeResponse | null;   // always null at login: the user picks on the supplier code screen
}
export interface ChangePasswordRequest { userName: string; currentPassword: string; newPassword: string; }
export interface SessionResponse { id: number; loginAt: string; lastActivityAt?: string | null; expiresAt: string; ipAddress?: string | null; userAgent?: string | null; isCurrent: boolean; }

// ---------- security question / forgot password ----------
export interface SecurityQuestionResponse { id: number; questionText: string; }
export interface SetSecurityQuestionRequest { questionId: number; answer: string; currentPassword: string; }
export interface ForgotPasswordStartRequest { userName: string; }
export interface ForgotPasswordStartResponse { requestToken: string; questionText: string; expiresAt: string; }
export interface ForgotPasswordVerifyRequest { requestToken: string; answer: string; }
export interface ForgotPasswordResetRequest { requestToken: string; newPassword: string; }

// ---------- supplier code / permissions ----------
export interface SupplierCodeResponse {
  id: number; companyId: number; companyName?: string | null; franchiseName?: string | null;
  exciseId: number; exciseCode: string; supplierCode: string;
  liquorCategoryId: number; liquorCategoryCode: string;
  displayName: string;                 // "RJ CL 772" — show this
  isActive: boolean; createdAt?: string | null;
}
export interface SelectSupplierCodeRequest { supplierCodeId: number; }
export interface MyPermissionsResponse { isSuperAdmin: boolean; activeSupplierCode: SupplierCodeResponse | null; permissions: string[]; }

// ---------- users ----------
export interface UserRoleAssignment { roleId: number; }             // the supplier code comes with the role
export interface UserRightAssignment { pageActionId: number; supplierCodeId: number | null; }   // null = all supplier codes
export interface CreateUserRequest {
  userName: string; password: string;
  companyId?: number | null;           // Admin only
  roles: UserRoleAssignment[];         // direct roles (may be empty when roleGroupIds is not)
  roleGroupIds: number[];              // role groups; at least one role OR one group
  fullName?: string; email?: string; phone?: string; employeeCode?: string;
  forcePasswordChange: boolean;        // default true
}
export interface UpdateUserRequest { fullName?: string; email?: string; phone?: string; employeeCode?: string; }
export interface UserResponse {
  id: number; userName: string; fullName?: string | null; email?: string | null; phone?: string | null;
  employeeCode?: string | null; companyId?: number | null; isActive: boolean; isBlocked: boolean;
  failedLoginAttempts: number; lockedUntil?: string | null; forcePasswordChange: boolean;
  passwordExpiresAt?: string | null; lastLoginAt?: string | null; createdAt?: string | null;
}
export interface UserRoleResponse { roleId: number; roleName: string; displayName: string; supplierCodeId: number | null; supplierCodeName: string; }
export interface UserRightResponse { pageActionId: number; permissionKey: string; actionName: string; supplierCodeId: number | null; supplierCodeName: string; }
export interface UserRoleGroupResponse { roleGroupId: number; groupName: string; roles: UserRoleResponse[]; }
export interface UserAccessResponse { userId: number; userName: string; roles: UserRoleResponse[]; roleGroups: UserRoleGroupResponse[]; rights: UserRightResponse[]; }
export interface UpdateUserRolesRequest { roles: UserRoleAssignment[]; }
export interface UpdateUserRoleGroupsRequest { roleGroupIds: number[]; }
export interface UpdateUserRightsRequest { rights: UserRightAssignment[]; }

// ---------- role groups ----------
export interface RoleGroupRoleResponse { roleId: number; roleName: string; displayName: string; supplierCodeId: number | null; supplierCodeName: string | null; isAdminRole: boolean; }
export interface RoleGroupResponse {
  id: number; companyId: number; groupName: string; description?: string | null; isActive: boolean;
  hasAdminRole: boolean;   // only user.manageadmin may change or give it
  userCount: number;       // users holding the group (in use → cannot be deleted)
  roles: RoleGroupRoleResponse[];
}
export interface SaveRoleGroupRequest { groupName: string; description?: string | null; }
export interface UpdateRoleGroupRolesRequest { roleIds: number[]; }   // the FULL list

// ---------- roles / pages ----------
export interface RoleResponse {
  id: number; companyId?: number | null;
  supplierCodeId?: number | null;      // null = company-level
  supplierCodeName?: string | null;    // "RJ CL 772"
  roleName: string;                    // "Operator"
  displayName: string;                 // "Operator RJ CL 772" — show this
  description?: string | null;
  isSystem: boolean; isTemplate: boolean; perSupplierCode: boolean; isAdminRole: boolean; isActive: boolean;
  passwordPolicyId?: number | null;
}
export interface SaveRoleRequest {
  roleName: string;
  supplierCodeId?: number | null;      // create: the role's supplier code (null = company-level); edit: the same value
  description?: string | null;
  isAdminRole: boolean;                // company-level only
  perSupplierCode?: boolean;           // Admin, templates only
  passwordPolicyId: number;
}
export type GrantScope = "ANY" | "ADMIN" | "SYSTEM";
export interface PageActionResponse { pageActionId: number; actionKey: string; permissionKey: string; actionName: string; grantScope: GrantScope; granted: boolean; }
export type ApplicationType = "WEB" | "LINE";
export interface PageResponse { pageId: number; pageKey: string; pageName: string; moduleName?: string | null; applicationType: ApplicationType; actions: PageActionResponse[]; }
export interface RoleRightsResponse { roleId: number; roleName: string; displayName: string; pages: PageResponse[]; }
export interface UpdateRoleRightsRequest {
  pageActionIds: number[];             // ALL ticked ids (of applicationType when sent, else of both applications)
  applicationType?: ApplicationType;   // send it when the grid shows one application
}

// ---------- settings ----------
export interface SecurityConfigResponse { key: string; value: string; dataType: "INT" | "BOOL" | "STRING"; description?: string | null; updatedAt: string; }
export interface UpdateSecurityConfigRequest { value: string; }
export interface PasswordPolicyResponse {
  id: number; policyName: string; minLength: number; maxLength: number;
  requireUppercase: boolean; requireLowercase: boolean; requireNumber: boolean; requireSpecialCharacter: boolean;
  passwordHistoryCount: number; passwordExpiryEnabled: boolean; passwordExpiryDays?: number | null;
  allowUsernameInPassword: boolean; allowCommonPassword: boolean; status: boolean; updatedAt: string;
}
export type UpdatePasswordPolicyRequest = Omit<PasswordPolicyResponse, "id" | "policyName" | "status" | "updatedAt">;

// ---------- dropdown lists ----------
export interface LiquorCategoryResponse { id: number; categoryCode: string; categoryName: string; description?: string | null; isActive: boolean; }
export interface ExciseResponse { id: number; exciseCode: string; exciseName: string; isActive: boolean; }
export interface CompanyResponse { id: number; companyName?: string | null; aliasName?: string | null; city?: string | null; isActive: boolean; supplierCodeCount: number; }
```

---

## B14. Test users (dev server)

Password of every demo user: `Admin@123`. Company: Globus Spirits Ltd (RJ CL 772, RJ IMFL 1028, JK IMFL 369).

| User | Roles | Supplier codes on the screen after login |
|---|---|---|
| `admin` | Admin | every supplier code of every company |
| `globus.admin` | Plant Admin (company-level) | 3 |
| `globus.agent` | Agent Manager (company-level) | 3 |
| `globus.pm` | Plant Manager RJ CL 772 / RJ IMFL 1028 / JK IMFL 369 | 3 |
| `rj.supervisor` | Supervisor RJ CL 772, Supervisor RJ IMFL 1028 | 2 |
| `rj772.operator` | Operator RJ CL 772 (+ custom right `user.view`) | 1 (still shown, user picks it) |
| `rj1028.operator` | Operator RJ IMFL 1028 | 1 (still shown, user picks it) |
| `jk369.operator` | Operator JK IMFL 369 | 1 (still shown, user picks it) |
| `globus.viewer` | Viewer of all 3 supplier codes | 3 |

- On the dev server `admin` logs in directly (no first-login steps), because the team and the automatic tests use it.
  On a new installation it starts with `Admin@123` and must change it and set a question at the first login.
- The demo users have no security question yet: their first login shows "Set security question" (good for testing it).
- To test the full first login, create a user with `forcePasswordChange: true` and log in as that user.

## B15. Known gaps (tell the backend team if they block you)

- **Agent Manager and the password-policy dropdown:** `GET /api/passwordpolicies` needs `passwordpolicy.view`, which only
  admins hold, so an Agent Manager cannot fill the role form's policy dropdown yet.
- **No delete-user API** — deactivate instead.
- **No company create / edit** — companies come from the CRM.
- **`SECURITY_QUESTION_REQUIRED`** above 1 has no effect yet (one question per user).
