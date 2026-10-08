# User Module — API Guide

A plain-language guide to every API the User module offers: what each one is for, who may call it,
what to send, what comes back, and what can go wrong. Written for the people building the React
screens, testers, and anyone new to the project.

---

## 1. Basics you need for every call

**Address.** Local run: `https://localhost:7180` (or `http://localhost:5180`). Every User-module
path starts with `/api/`. Swagger (try-it-out page): `https://localhost:7180/swagger`.

**Who can call what.** Each API is one of three kinds:

| Label in this guide | Meaning |
|---|---|
| 🌐 **Public** | No login needed. |
| 🔑 **Logged in** | Desktop / scripts / Swagger: header `Authorization: Bearer <accessToken>`. Web app: nothing by hand - login also set the HttpOnly `jwt` cookie, which the browser sends on its own; on POST/PUT/DELETE axios adds `X-XSRF-TOKEN` from the readable `XSRF-TOKEN` cookie. The header wins when both are present. |
| 🛡️ **Right** | Logged in **and** the user holds the named permission key (e.g. `user.add`) in the selected supplier code, through a role or a custom right. Admin holds every key. See `06-roles-rights-plan.md`. |

**The token.** Login gives you an `accessToken`. The server also keeps a record of it (a "session").
A session ends in one of three ways, and each one needs a different screen:

| 401 errorCode | Why | What the screen does |
|---|---|---|
| `SESSION_TIMED_OUT` | No API call for `idleTimeoutMinutes` (default 60). Every call restarts this clock. | Go to the **login page** |
| `SESSION_EXPIRED` | The hard limit `expiresAt` (default 24 h after login) was reached, even while working | Show a **password popup** on the same screen. Call `login` with the same user name, keep the new token, and **send the failed call again**. Nothing on the screen is lost. |
| `SESSION_INVALID` | Logged out, revoked, deactivated, or password changed | Go to the **login page** |

Tip: `expiresAt` from the login response lets the screen warn *"your session ends in 10 minutes"*
before it happens. A call that has already started (for example a slow report) always finishes, even if
the session ends while it runs.

**Times.** All dates and times are **Indian time (IST)**, sent without a time zone, for example
`2026-09-28T18:30:00`. Show them as they are. Do not convert them.

**JSON names** are camelCase: `userName`, `accessToken`, `supplierCodeId`.

**Errors.** Every error has the same shape. Build the screen logic on `errorCode`, never on the
message text:

```json
{
  "type": "https://errors.stliquortnt.local/user_locked",
  "status": 403,
  "errorCode": "USER_LOCKED",
  "title": "Account locked after 3 failed attempts.",
  "detail": "Try again after 2026-09-29 10:07 or ask an administrator to unlock.",
  "instance": "/api/auth/login",
  "correlationId": "6f1c…"
}
```

| Key | Use it for |
|---|---|
| `type` | an identifier for the kind of error, as a URL. Not used by the screens yet. |
| `status` | the HTTP status, repeated in the body |
| `errorCode` | **the screen logic.** It never changes, while the text may. |
| `title` | the short message to show the user |
| `detail` | what to do next. It can be `null` (for example on a wrong password, where saying more would help an attacker). |
| `instance` | which API failed, handy in a screenshot |
| `correlationId` | the reference to give support; it finds the call in the server log |

Every error has these seven keys, in this order.

When a form field is wrong, the error also carries `errors`, a list of problems for each field:

```json
{ "status": 400, "errorCode": "VALIDATION_FAILED",
  "errors": { "password": ["Password must be at least 12 characters.", "Password must contain a number."] } }
```

When a user reports a problem, ask them for the `correlationId`. It points to the exact entry in the
server log.

**Every response has a body, success or error.** Nothing ever answers with an empty body.
- An action on a user (unlock, activate …) returns that user after the change.
- An action with nothing to return (logout, verify, reset …) returns a message:
  `{ "message": "Logged out." }`.
- A server error (500) shows what actually broke. `detail` holds the chain of causes, for example
  `DbUpdateException: … --> MySqlException: Table 'st_tnt_liquor.users' doesn't exist`. The body
  also has `exceptionType` and `stackTrace`, so the response alone is enough to start
  troubleshooting.
- Mistakes in the call itself are explained too: a wrong address gets `404 ENDPOINT_NOT_FOUND`, the
  wrong HTTP method gets `405 METHOD_NOT_ALLOWED`, and a body that is not JSON gets
  `415 UNSUPPORTED_MEDIA_TYPE`.

**Addresses** are all lowercase with no `-`: `/api/auth/changepassword`,
`/api/auth/forgotpassword/start`, `/api/securityquestions`.

---

## 2. All APIs at a glance

| # | Method & path | Who | Used for |
|---|---|---|---|
| 1 | `POST /api/auth/login` | 🌐 | Log in, get a token |
| 2 | `POST /api/auth/logout` | 🔑 | Log out of this device |
| 3 | `GET /api/auth/me` | 🔑 | Who am I? (header / profile) |
| 4 | `POST /api/auth/changepassword` | 🌐 | Change password (also the forced first-login change) |
| 5 | `GET /api/auth/sessions` | 🔑 | My logged-in devices |
| 6 | `DELETE /api/auth/sessions/{id}` | 🔑 | Log out one of my other devices |
| 7 | `GET /api/securityquestions` | 🌐 | List of security questions to choose from |
| 8 | `PUT /api/securityquestions/mine` | 🔑 | Set or change my security question and answer |
| 9 | `POST /api/auth/forgotpassword/start` | 🌐 | Forgot password, step 1: get my question |
| 10 | `POST /api/auth/forgotpassword/verify` | 🌐 | Forgot password, step 2: check my answer |
| 11 | `POST /api/auth/forgotpassword/reset` | 🌐 | Forgot password, step 3: set a new password |
| 12 | `POST /api/users` | 🛡️ | Create a user |
| 13 | `GET /api/users` | 🛡️ | User list (search + pages) |
| 14 | `GET /api/users/{id}` | 🛡️ | One user's details |
| 15 | `PUT /api/users/{id}` | 🛡️ | Edit a user's details / role / company |
| 16 | `POST /api/users/{id}/activate` | 🛡️ | Turn a user back on |
| 17 | `POST /api/users/{id}/deactivate` | 🛡️ | Turn a user off (logs them out everywhere) |
| 18 | `POST /api/users/{id}/unlock` | 🛡️ | Remove a lock or block |
| 19 | `GET /api/securityconfig` | 🛡️ | Read security settings |
| 20 | `PUT /api/securityconfig/{key}` | 🛡️ | Change one security setting |
| 21 | `GET /api/passwordpolicies` | 🛡️ | Read password rules (EASY / MEDIUM / HARD) |
| 22 | `PUT /api/passwordpolicies/{id}` | 🛡️ | Change a password rule set |
| 23 | `GET /health` | 🌐 | Is the server running? |

---

## 3. Common screen flows

**Normal login**
`login` → store `accessToken` → call other APIs with it → `logout` when done.

**First login with a temporary password** (the admin created the user with `forcePasswordChange: true`)
1. `login` answers `403 PASSWORD_CHANGE_REQUIRED`.
2. Show the "Set new password" screen, then call `changepassword` with the user name, the
   temporary password and the new password.
3. Call `login` again with the new password.

The same flow applies to `403 PASSWORD_EXPIRED`.

**Forgot password**
`forgotpassword/start` (user name) → show the question → `verify` (answer) → `reset` (new
password) → `login`. The `requestToken` from step 1 is used in steps 2 and 3.

**Too many wrong passwords**
With the default settings, 3 wrong passwords in one day lock the account for 24 hours. While
locked, even the right password gets `403 USER_LOCKED`. The lock goes away when it expires, when an
admin calls `unlock`, or when the user completes forgot password.

**Session ends while the user is working (hard limit)**
Put this in one place in the app (an Axios response interceptor), not in every screen:
1. Any call answers `401 SESSION_EXPIRED`.
2. Keep the screen as it is. Show a popup: "For security, enter your password again".
3. Call `POST /api/auth/login` with the stored user name and the typed password. Store the new
   `accessToken` and `expiresAt`.
4. Send the call that failed again with the new token. Calls that failed while the popup was open
   wait and are sent too.
5. On `SESSION_TIMED_OUT` or `SESSION_INVALID`, clear the token and open the login page instead.

**"Too many devices"**
With the default settings, a user can be logged in on 2 devices. A third login gets
`409 SESSION_LIMIT_REACHED`. The user must log out from another device first. From a logged-in
device, `GET /api/auth/sessions` + `DELETE /api/auth/sessions/{id}` do this.

---

## 4. Each API in detail

### 4.1 Login and my account

#### 1. `POST /api/auth/login` 🌐
Checks the user name and password. On success it opens a session and returns a token.

```json
// request
{ "userName": "admin", "password": "Admin@123" }

// 200 response
{
  "accessToken": "eyJhbGciOi…",
  "expiresAt": "2026-09-29T10:00:00",     // hard limit of this session (then SESSION_EXPIRED → popup)
  "idleTimeoutMinutes": 60,               // no call for this long → SESSION_TIMED_OUT → login page
  "user": { "userId": 1, "userName": "admin", "fullName": "Administrator",
            "companyId": null, "forcePasswordChange": false, "passwordExpiresAt": null },
  "supplierCodes": [ { "id": 3, "exciseCode": "RJ", "supplierCode": "550", "liquorCategoryCode": "CL", "displayName": "RJ CL 550", … } ],
  "activeSupplierCode": null           // always null: after every login the user picks a supplier code (POST /api/auth/selectsuppliercode)
}
```

| Error | When |
|---|---|
| 400 `VALIDATION_FAILED` | User name or password empty, or too long |
| 401 `INVALID_CREDENTIALS` | Wrong user name **or** wrong password. Both get the same answer on purpose, so nobody can find out which user names exist. |
| 403 `USER_LOCKED` | Too many wrong passwords today. The attempt that locks the account says *"Account locked after 3 failed attempts."*; any later attempt says *"This account is already locked."*. In both, `detail` gives the unlock time. |
| 403 `USER_INACTIVE` / `USER_BLOCKED` | Admin has turned the user off or blocked them |
| 403 `PASSWORD_CHANGE_REQUIRED` / `PASSWORD_EXPIRED` | The password is right, but a new one must be set first (see §3) |
| 409 `SESSION_LIMIT_REACHED` | Already logged in on the maximum number of devices |

The `200` also carries `Set-Cookie: jwt=<accessToken>; HttpOnly; Secure; SameSite=Lax; Path=/; Max-Age=…` and
`Set-Cookie: XSRF-TOKEN=<random>; Secure; SameSite=Lax; Path=/; Max-Age=…`. Browsers use them; other clients ignore them.

A wrong password adds to the day's failed-attempt count. A successful login resets the count to 0.

#### 2. `POST /api/auth/logout` 🔑
Ends the session of the token you send (header, or else the cookie) and expires both cookies. That token stops working at once. Other devices stay
logged in.
Response: `200` → `{ "message": "Logged out." }`.

#### 3. `GET /api/auth/me` 🔑
Returns the logged-in user, in the same shape as `user` in the login response. Use it to fill the
header or profile, or to check that a stored token still works.

#### 4. `POST /api/auth/changepassword` 🌐
Changes the password. This API is public because the forced first-login change happens **before**
the user can log in. The current password proves who the user is.

```json
{ "userName": "ravi", "currentPassword": "Temp@12345", "newPassword": "MyNew#Pass2026" }
```
Response: `200` → `{ "message": "Password changed. All sessions have been ended; log in with the new password." }`.
After this, **every session of the user is ended**, so the user has to log in again.

| Error | When |
|---|---|
| 400 `VALIDATION_FAILED` | New password breaks the rules of the user's role (`errors.password` lists each broken rule), is the same as the current one, or was used recently |
| 401 `INVALID_CREDENTIALS` | Wrong user name or current password. This **counts as a failed login**. |
| 403 `USER_LOCKED` / `USER_INACTIVE` / `USER_BLOCKED` | Same as login |

#### 5. `GET /api/auth/sessions` 🔑
Lists the devices where the user is logged in right now. `isCurrent: true` marks the device making
the call.

```json
[ { "id": 41, "loginAt": "2026-09-28T09:00:00", "lastActivityAt": "2026-09-28T09:40:00",
    "expiresAt": "2026-09-29T09:00:00", "ipAddress": "192.168.1.20",
    "userAgent": "Mozilla/5.0 …", "isCurrent": true } ]
```

#### 6. `DELETE /api/auth/sessions/{id}` 🔑
Logs out one of **your own** sessions, for example "log out my other device".
Response: `200` → `{ "message": "Session 41 has been logged out." }`, or `"… had already ended."` if it
was already over.
`404 NOT_FOUND` means the session does not belong to you.

### 4.2 Security question

Each user has **one** security question. They use it to reset a forgotten password. There are 10 questions
(`db/mysql/014`). A question that any user has answered can never change its text (database trigger) or be deleted
(foreign key); it can only be retired (`STATUS = 0`), and users who chose it keep it.

#### 7. `GET /api/securityquestions` 🌐
The list of questions to pick from: `[ { "id": 1, "questionText": "What is the name of your first school?" }, … ]`

#### 8. `PUT /api/securityquestions/mine` 🔑
Sets or changes the logged-in user's question and answer. The current password must be sent as a
check.

```json
{ "questionId": 3, "answer": "Tommy", "currentPassword": "MyNew#Pass2026" }
```
Response: `200` → `{ "message": "Security question saved: \"What was the name of your first pet?\"" }`.
- Picking a different question replaces the old one.
- The answer ignores upper/lower case and **all** spaces: `"New Delhi"`, `"newdelhi"` and `"  NEW  DELHI "` count as the
  same answer. Spelling and punctuation still count (`"St. Mary's"` ≠ `"St Marys"`). At least 2 characters, not
  counting spaces.
- A wrong `currentPassword` gives `401 INVALID_CREDENTIALS` and counts as a failed login.
- An unknown or disabled `questionId` gives `404`.

### 4.3 Forgot password (3 steps, all 🌐)

#### 9. `POST /api/auth/forgotpassword/start`
```json
// request
{ "userName": "ravi" }
// 200 response
{ "requestToken": "k3J9…", "questionText": "What was the name of your first pet?", "expiresAt": "2026-09-28T10:15:00" }
```
Keep `requestToken` in memory for the next two steps. It is valid for **15 minutes** (a setting).
Calling `start` again cancels the earlier request.

| Error | When |
|---|---|
| 409 `SECURITY_QUESTION_NOT_SET` | No question set for this user. An unknown user name gets the same answer. The user must ask an admin. |
| 409 `SECURITY_QUESTION_DISABLED` | Forgot password is switched off in settings |
| 403 `USER_LOCKED` / `USER_INACTIVE` / `USER_BLOCKED` | A locked or turned-off account cannot use forgot password |

#### 10. `POST /api/auth/forgotpassword/verify`
```json
{ "requestToken": "k3J9…", "answer": "tommy" }
```
Response when the answer is right: `200` →
`{ "message": "Answer verified. Set a new password before 2026-09-28 10:15." }`.

| Error | When |
|---|---|
| 401 `SECURITY_ANSWER_INCORRECT` | Wrong answer. This **also counts as a failed login**, so guessing answers locks the account just like guessing passwords. |
| 403 `RESET_ATTEMPTS_EXCEEDED` | Too many wrong answers on this request (default 5). Start again. |
| 403 `USER_LOCKED` | The wrong answers have locked the account |
| 409 `RESET_REQUEST_EXPIRED` / `RESET_REQUEST_INVALID` | Token is too old, already used, or wrong. Start again. |

#### 11. `POST /api/auth/forgotpassword/reset`
```json
{ "requestToken": "k3J9…", "newPassword": "Fresh#Pass2026" }
```
Response: `200` → `{ "message": "Password has been reset. Log in with the new password." }`.
The new password must follow the role's rules (`400` with `errors.password`
otherwise). A successful reset also clears the failed-login lock and ends all the user's sessions.
The user then logs in with the new password. A token works only once.

### 4.4 User management (🛡️ rights: `user.view`, `user.add`, `user.edit`, `user.status`, `user.unlock`)

#### 12. `POST /api/users` — create a user
```json
{
  "userName": "ravi.k",
  "password": "Temp@Pass2026x",
  "roles": [ { "roleId": 12, "supplierCodeId": 3 }, { "roleId": 14, "supplierCodeId": null } ],
  "fullName": "Ravi Kumar",
  "email": "ravi@example.com",
  "phone": "9876543210",
  "employeeCode": "EMP-104",
  "forcePasswordChange": true
}
```
Response: `201` with the new user (shape in #14).
- `userName`: up to 50 characters, only letters, digits and `. _ @ -`. It must be unique and
  **cannot be changed later**.
- `roles`: at least one. Each role is for one supplier code (`supplierCodeId`) or for every supplier code of the company (`null`).
  The roles and supplier codes must belong to the caller's company; giving an admin role (Plant Admin) needs `user.manageadmin`.
- The new user always joins the caller's company. Only Admin may send `companyId` (to create the first user of a company).
- `password` must follow the password rules of the user's roles (with several roles, the strictest value of each rule).
- `forcePasswordChange` is `true` by default: the user must set their own password at first login.
- `fullName`, `email`, `phone`, `employeeCode` are optional.

| Error | When |
|---|---|
| 400 `VALIDATION_FAILED` | Bad field, or the password breaks the role's rules (`errors.password`) |
| 404 `NOT_FOUND` | A role or supplier code is not of this company, or `companyId` does not exist |
| 403 `ADMIN_USER_PROTECTED` | An admin role was given without `user.manageadmin` |
| 409 `USERNAME_TAKEN` | User name already used |
| 409 `PASSWORD_POLICY_NOT_CONFIGURED` | The role has no password rules linked to it (see §6) |

#### 13. `GET /api/users?search=ravi&page=1&pageSize=50`
Returns users page by page. `search` matches part of the user name or full name. `pageSize` is 50
by default and 200 at most.

```json
{ "items": [ { …user… } ], "page": 1, "pageSize": 50, "totalCount": 132 }
```

#### 14. `GET /api/users/{id}`
```json
{ "id": 7, "userName": "ravi.k", "fullName": "Ravi Kumar", "email": "ravi@example.com",
  "phone": "9876543210", "employeeCode": "EMP-104", "companyId": 5,
  "isActive": true, "isBlocked": false, "failedLoginAttempts": 0, "lockedUntil": null,
  "forcePasswordChange": true, "passwordExpiresAt": null,
  "lastLoginAt": "2026-09-28T09:00:00", "createdAt": "2026-09-27T16:20:00" }
```
`404` if the user does not exist or belongs to another company. Roles and rights: `GET /api/users/{id}/access`. The password is never returned, not even as a hash.
`failedLoginAttempts` is the number of wrong passwords counted today. Together with `lockedUntil`
it shows the admin why a user is locked.

#### 15. `PUT /api/users/{id}` — edit a user
```json
{ "fullName": "Ravi Kumar", "email": "ravi@example.com",
  "phone": "9876543210", "employeeCode": "EMP-104" }
```
Send **all** fields. A field left out is cleared, it does not keep its old value. The user name and
password cannot be changed here, and neither can roles (use `PUT /api/users/{id}/roles`, see 06). Response: `200` with the updated user.
Editing, deactivating or unlocking an **admin user** needs `user.manageadmin` (`403 ADMIN_USER_PROTECTED`).

These three send no body. Each one answers `200` with the user **after** the change (same shape as
#14), so the screen can refresh the row straight away.

#### 16. `POST /api/users/{id}/activate`
Turns the user back on. The response shows `isActive: true`.

#### 17. `POST /api/users/{id}/deactivate`
Turns the user off. **All their sessions end immediately**, and they cannot log in until
reactivated. Nothing is deleted. The response shows `isActive: false`.
`409 CANNOT_DEACTIVATE_SELF` means an admin tried to turn off their own account.

#### 18. `POST /api/users/{id}/unlock`
Removes the failed-login lock **and** an admin block, and sets the failed-attempt count back to 0.
The response shows `lockedUntil: null`, `isBlocked: false`, `failedLoginAttempts: 0`. Calling it on
a user who is not locked is harmless.

### 4.5 Security settings and password rules (🛡️ rights: `securityconfig.view/edit`, `passwordpolicy.view/edit`)

#### 19. `GET /api/securityconfig`
All settings: `[ { "key": "MAX_FAILED_LOGIN_ATTEMPTS", "value": "3", "dataType": "INT", "description": "…", "updatedAt": "…" }, … ]`

#### 20. `PUT /api/securityconfig/{key}`
```json
// PUT /api/securityconfig/MAX_FAILED_LOGIN_ATTEMPTS
{ "value": "5" }
```
Response: `200` with the updated setting. It **takes effect from the very next request**, with no
restart.
The value is checked against its type:
- `INT` must be a whole number, 1 or more.
- `BOOL` must be `1`/`0` or `true`/`false`.
- `SESSION_FULL_BEHAVIOUR` accepts only `REJECT`.

A bad value gives `400` with `errors.value`. An unknown key gives `404`.

| Key | Default | What it controls |
|---|---|---|
| `FAILED_LOGIN_LOCK_ENABLED` | `1` | Lock accounts after too many wrong passwords (on/off) |
| `MAX_FAILED_LOGIN_ATTEMPTS` | `3` | Wrong passwords **in one day** before the lock |
| `ACCOUNT_LOCK_DURATION_MINUTES` | `1440` | How long the lock lasts (1440 = 24 hours) |
| `SESSION_LIMIT_ENABLED` | `1` | Limit how many devices one user can be logged in on (on/off) |
| `MAX_ACTIVE_SESSIONS` | `2` | That limit |
| `SESSION_IDLE_MINUTES` | `60` | No API call for this long ends the session (`SESSION_TIMED_OUT` → login page). Every call restarts the clock. |
| `SESSION_EXPIRY_MINUTES` | `1440` | Hard limit of a session, even while working (`SESSION_EXPIRED` → password popup, then retry) |
| `SESSION_FULL_BEHAVIOUR` | `REJECT` | What happens at the limit: the new login is refused |
| `SECURITY_QUESTION_ENABLED` | `1` | Forgot password by security question (on/off) |
| `SECURITY_QUESTION_REQUIRED` | `1` | Questions per user |
| `PASSWORD_RESET_EXPIRY_MINUTES` | `15` | How long a forgot-password request stays valid |
| `PASSWORD_RESET_MAX_ATTEMPTS` | `5` | Wrong answers allowed per forgot-password request |
| `LOG_MODE` | `NORMAL` | How much the server writes to its log. `NORMAL`: one line per call, plus errors. `DETAIL`: also every request and response body, every method's input and output, and the SQL. The change applies within 10 seconds, with no restart. See §7. |

#### 21. `GET /api/passwordpolicies`
The rule sets, for example:

| Policy | Min–max length | Uppercase | Lowercase | Number | Special | Remembers last | Expires |
|---|---|---|---|---|---|---|---|
| EASY | 6–20 | – | – | ✔ | – | 3 | never |
| MEDIUM | 8–30 | ✔ | ✔ | ✔ | – | 5 | never |
| HARD | 12–64 | ✔ | ✔ | ✔ | ✔ | 5 | 90 days |

Every policy also refuses a password that contains the user name, or a very common password
(`admin@123`, `password1`, …), unless the admin allows it.

#### 22. `PUT /api/passwordpolicies/{id}`
```json
{ "minLength": 10, "maxLength": 64, "requireUppercase": true, "requireLowercase": true,
  "requireNumber": true, "requireSpecialCharacter": true, "passwordHistoryCount": 5,
  "passwordExpiryEnabled": true, "passwordExpiryDays": 60,
  "allowUsernameInPassword": false, "allowCommonPassword": false }
```
Send **all** fields. Response: `200` with the updated policy.
- Lengths must be 1–128, and the max must not be below the min.
- History must be 0–50.
- When expiry is on, `passwordExpiryDays` must be 1 or more.

New rules apply the next time a password is set. Existing passwords keep working.

### 4.6 Other

#### 23. `GET /health` 🌐
`{ "status": "ok" }`. Use it for uptime checks.

---

## 5. Error codes — what the screen should do

| errorCode | Status | Show / do |
|---|---|---|
| `VALIDATION_FAILED` | 400 | Show `errors` next to each field |
| `INVALID_CREDENTIALS` | 401 | "User name or password is incorrect" |
| `UNAUTHENTICATED` | 401 | No or bad token: go to login |
| `SESSION_INVALID` | 401 | Logged out / revoked: go to login |
| `SESSION_TIMED_OUT` | 401 | Idle too long: go to login |
| `SESSION_EXPIRED` | 401 | Hard limit reached: password popup, log in again, retry the call |
| `SECURITY_ANSWER_INCORRECT` | 401 | "Answer is incorrect" |
| `USER_LOCKED` | 403 | Show `detail` (it has the unlock time) |
| `USER_INACTIVE` / `USER_BLOCKED` | 403 | "Contact your administrator" |
| `PASSWORD_CHANGE_REQUIRED` / `PASSWORD_EXPIRED` | 403 | Open the "Set new password" screen |
| `RESET_ATTEMPTS_EXCEEDED` | 403 | "Too many wrong answers — start again" |
| `PERMISSION_DENIED` | 403 | The user lacks the permission key (`detail` names it): hide that menu / button using `GET /api/auth/mypermissions` |
| `SUPPLIER_CODE_NOT_SELECTED` | 409 | No supplier code picked for the session: show the supplier code picker |
| `ADMIN_USER_PROTECTED` / `RIGHT_NOT_GRANTABLE` | 403 | Only an administrator (or nobody) may give this role / right |
| `NOT_FOUND` | 404 | "Not found" |
| `USERNAME_TAKEN` | 409 | "User name already in use" |
| `PASSWORD_POLICY_NOT_CONFIGURED` | 409 | "This role has no password rules — ask IT" |
| `CANNOT_DEACTIVATE_SELF` / `CANNOT_CHANGE_OWN_ACCESS` | 409 | Explain that nobody can do this to their own account |
| `SESSION_LIMIT_REACHED` | 409 | "Log out from another device first" |
| `CSRF_REJECTED` | 403 | A cookie-authenticated write without a matching `X-XSRF-TOKEN`, or from another site. A client bug, not a user error. |
| `SECURITY_QUESTION_NOT_SET` / `SECURITY_QUESTION_DISABLED` | 409 | "Ask the administrator to reset your password" |
| `RESET_REQUEST_EXPIRED` / `RESET_REQUEST_INVALID` | 409 | "Start forgot password again" |
| `ENDPOINT_NOT_FOUND` / `METHOD_NOT_ALLOWED` / `UNSUPPORTED_MEDIA_TYPE` | 404 / 405 / 415 | A bug in the calling code: wrong URL, wrong method, or the body was not sent as JSON |
| `REQUEST_INVALID` / `REQUEST_REFUSED` | 4xx | The server could not read the request (for example, the body is too large), or refused it for another reason. `detail` says which. |
| `DATABASE_ERROR` / `UNEXPECTED_ERROR` | 500 | "Something went wrong", with the `correlationId`; `detail` + `stackTrace` tell a developer what broke |

---

## 6. Good to know (current limits)

- **Every change is recorded** in `USER_LOG`: who, what, when, from which IP. Passwords, answers
  and tokens are never written there.
- **Roles, rights and supplier codes** are in `06-roles-rights-plan.md`: a role is created with its password policy
  (`passwordPolicyId`), and a user works inside one supplier code at a time.
- **Forgot password is only by security question.** There is no email or SMS OTP.
- **Test login:** `admin` / `Admin@123`. Change it on every customer installation.

---

## 7. Server logs (for support)

The log is written to `logs/stliquortnt-YYYYMMDD.json` next to the API, and also to the console. On a
customer server, set a full path in `appsettings.json`, for example `D:\STLiquorTNT\logs\stliquortnt-.json`.

### Every entry has the same keys

```json
{
  "Timestamp": "2026-09-28 18:44:06.341",
  "Level": "Warning",
  "CorrelationId": "6c0a6e291d304e8bb08b719b5fd74141",
  "Method": "AuthService.LoginAsync",
  "Message": "Refused: INVALID_CREDENTIALS User name or password is incorrect. (91 ms)",
  "Context": { "input": { "request": { "userName": "ravi", "password": "***" } } }
}
```

| Key | Meaning |
|---|---|
| `Timestamp` | Indian time |
| `Level` | `Info`, `Warning`, `Error` or `Fatal` |
| `CorrelationId` | The one API call this entry belongs to. It is `null` for start-up and background entries. |
| `Method` | Who wrote the entry: `AuthController.LoginAsync` (the HTTP call), `AuthService.LoginAsync` (a service method), `SQL`, `Startup`, or `LogModeRefresher` |
| `Message` | What happened, in one sentence |
| `Context` | The details: `input` / `output` of a method, `request` / `response` of a call, `ipAddress`, `userId`, and `sql` as lines. It is `null` when there is nothing more. |
| `Exception` | Present only on an error: `ExceptionType`, `ExceptionMessage`, `InnerExceptionType`, `InnerException` (the real cause), and `StackTrace` (first 20 lines) |

### What you will see

- **When the API starts** (IIS, Windows service or Visual Studio):
  - `API starting`, with the version, environment, machine, and database server and name. The connection string itself is never logged.
  - `Now listening on …`.
  - `Database reachable. Log mode is Normal.`: this proves the API can reach its database.
  - If the database is down or the configuration is wrong: `Database check failed: …`, with the exception.
  - If the API cannot start at all: a `Fatal` entry `API failed to start: …`, with the exception. For example, the port is already in use, or a setting is invalid.
- **NORMAL** (default): one entry per API call (`/api/...`), plus warnings and errors. Example:
  `"Method": "AuthController.LoginAsync", "Message": "POST /api/auth/login -> 401 INVALID_CREDENTIALS in 103 ms"`.
  Swagger and `/health` are not logged.
- **DETAIL** also logs these, in order, for each call:
  - every `SQL` it ran;
  - every service method, with `input` and `output` (`OK`), or with the reason (`Refused` / `Failed`);
  - the call entry itself, with `request` and `response`.

  Turn it on with `PUT /api/securityconfig/LOG_MODE` and body `{ "value": "DETAIL" }`. It applies
  within 10 seconds. Repeat the problem, then set it back to `NORMAL`.

### Find one call quickly

1. Take the `correlationId` from the error response (or the `X-Correlation-Id` response header).
2. Search the log file for it. Every entry of that call has the same value, top to bottom: SQL, then
   service method, then the call.
3. With no ID: search by `Timestamp`, the path (`/api/auth/login`), the `errorCode`, or `ipAddress`.

**Secrets never appear in the log, in either mode.** Password, answer, token and hash fields show
`***`. A body that is not valid JSON is not written at all. Switch DETAIL off when you are done: it
writes several entries per call, files roll at 100 MB, and only the last 30 files are kept.
