# Roles, Rights & Supplier codes

| | |
|---|---|
| **Status** | Implemented 2026-10-07 (backend). Schema: `db/mysql/009_roles_rights.sql`; roles per supplier code since 2026-10-08 (`012_supplier_code_roles.sql`) |
| **Scope** | Backend API only. The React app is built separately against the APIs in section 7 |
| **Replaced** | The interim `Administrator` policy (`SECURITY_CONFIG.ADMIN_ROLE_ID`) and the `role_id` JWT claim |
| **Not in this module** | Batch create / approve / cancel. It comes with the batch page and only adds rows to `PAGE_ACTIONS` (section 9) |

---

## 1. Decisions (owner, 2026-10-07)

| Topic | Decision |
|---|---|
| Model | ERPNext-style. A user holds **several master roles** plus **custom (user-specific) rights**. Effective rights = the union |
| Custom rights | **Grant only**: they add rights and never take away a right a role gives |
| Role ownership | **Company-wise.** The **Admin** (the `admin` user, system role "Admin", script 015) stands above all companies. The Super Admin of Sundaram Technologies is a separate, later feature and is not part of this module (owner, 2026-10-08) |
| Supplier code scope | **Roles belong to a supplier code** (owner, 2026-10-08): "Operator RJ CL 550", "Operator RJ FL 620" — each supplier code can have its own rights. Company-level roles (Plant Admin, Agent Manager; `SUPPLIER_CODE_ID` null) cover every supplier code. *Ramesh → Operator RJ CL 550* (script 012) |
| Default roles | Templates. Plant Admin, Agent Manager are copied once per company (with its first supplier code); Plant Manager, Supervisor, Operator, Viewer (`PER_SUPPLIER_CODE`) are copied for **every new supplier code**. The company may edit or delete its copies |
| Agent Manager | Creates users, roles and rights for everyone **except admin users** |
| Self-change | Nobody except Admin changes their **own** roles or rights |
| Role groups | A **role group** bundles master roles of one company; a user gets roles directly, through groups, or both (owner, 2026-10-08, script 016). A new supplier code's roles are ticked into groups by hand (not automatic); a group in use cannot be deleted; groups may hold admin roles under the `user.manageadmin` rule |
| Password policy | With several roles, the **strictest** value of each rule applies |
| Plant | No plant entity. Hierarchy **Company → Excise → Supplier Code**. "Plant Admin" / "Plant Manager" are role names only |
| Masters | `COMPANY`, `SUPPLIER_CODE` and `LIQUOR_CATEGORY` are maintained by Admin (they come from the CRM) |
| Generic actions | A page can have any actions, not only view/add/edit/delete. Approve / reject / cancel etc. are added per page when that page is built |
| Two applications | Pages belong to the **web** application (server) or the **line** application (desktop at the line) — `PAGES.APPLICATION_TYPE`, never both (a job in both = two pages). Roles and rights are shared; no login restriction per application yet (owner, 2026-10-08) |

---

## 2. Tenant model

```
Admin (the `admin` user, no company)
└─ COMPANY (customer, e.g. Globus)
   └─ SUPPLIER_CODE = EXCISE + code + LIQUOR_CATEGORY      e.g. RJ · 550 · CL
```

- After login the user works in one **active supplier code** (`USER_SESSION.ACTIVE_SUPPLIER_CODE_ID`). Nothing is picked at login (owner,
  2026-10-08): after every login every user, also with one supplier code, picks one with `POST /api/auth/selectsuppliercode`.
- `ITenantContext` = `CompanyId`, `SupplierCodeId`, `ExciseCode`. `SessionValidationMiddleware` loads the supplier code from the session; a
  deactivated supplier code counts as not picked. Tenant values never come from a request.

---

## 3. Tables (`db/mysql/009_roles_rights.sql`)

| Table | What it holds |
|---|---|
| `LIQUOR_CATEGORY` | `CATEGORY_CODE` (unique), `CATEGORY_NAME`, `DESCRIPTION`. Seeded with IMFL, CL, FL |
| `SUPPLIER_CODE` | One supplier code: `COMPANY_ID`, `FRANCHISE_NAME` (text), `EXCISE_ID`, `SUPPLIER_CODE`, `LIQUOR_CATEGORY_ID`. **Unique (excise, code)** |
| `COMPANY` | `EXCISE_CODE` and `SUPPLIER_CODE` moved out to `SUPPLIER_CODE`; company name is unique |
| `PAGES` | One screen (`PAGE_KEY`, `MODULE_NAME`, `SORT_ORDER`, `APPLICATION_TYPE` = `WEB` / `LINE` (013): the web application or the line application). `PAGE_NAME` unique per application, `PAGE_KEY` unique overall |
| `PAGE_ACTIONS` | The actions of a page. `PERMISSION_KEY` = `<page>.<action>` (unique); `GRANT_SCOPE` = `ANY` / `ADMIN` / `SYSTEM` |
| `ROLES` | + `COMPANY_ID`, `SUPPLIER_CODE_ID` (012; null = company-level), `IS_SYSTEM` (Admin), `IS_TEMPLATE` (default role), `PER_SUPPLIER_CODE` (012; templates copied per supplier code), `IS_ADMIN_ROLE` (Plant Admin, always company-level). Name is unique per company + supplier code |
| `ROLE_RIGHTS` | Rebuilt: one row = role has one page action |
| `USER_ROLES` | User → role. Since 012 the supplier code comes from the role (column removed); unique (user, role) |
| `ROLE_GROUP` | (016) A named bundle of roles of one company: `COMPANY_ID`, `GROUP_NAME` (unique per company), `DESCRIPTION`, `IS_ACTIVE` |
| `ROLE_GROUP_ROLES` | (016) Group → role. A role inside a group cannot be deleted (FK) |
| `USER_ROLE_GROUPS` | (016) User → group. A user holds every role of their active groups |
| `USER_RIGHTS` | Custom rights: user → page action → supplier code (null = all supplier codes) |
| `USER_SESSION` | + `ACTIVE_SUPPLIER_CODE_ID` |
| `USERS` | `ROLE_ID` removed (moved to `USER_ROLES`) |
| `SECURITY_CONFIG` | `ADMIN_ROLE_ID` removed |

**Status values vs lookup tables:** a value the code branches on (`GRANT_SCOPE`) is a C# enum stored as text, without a table. A
business category that grows as data (`LIQUOR_CATEGORY`) gets its own table with a foreign key. There is no generic LOOKUP table.

---

## 4. How a call is authorised

```
[HasPermission("user.add")]  →  PermissionHandler  →  CurrentAccess.EnsurePermissionAsync
   Admin                       → allowed (every key, every company)
   no supplier code picked                 → 409 SUPPLIER_CODE_NOT_SELECTED
   key not in roles+rights of the    → 403 PERMISSION_DENIED (detail names the key)
   active supplier code
```

- Effective keys = rights of the user's roles (given directly **or through a role group**) of the active supplier code and company-level roles ∪ custom rights for it (or all supplier codes). One query defines the held roles (`Infrastructure/.../UserRoleQuery`), used for rights, the picker, the password policy and the admin-user check.
  A company-level role or "all supplier codes" right only applies to supplier codes of the user's **own** company; the query checks this again.
- Rights are read **per request** (one query, kept by the scoped `CurrentAccess`). They are not in the JWT, so a change applies on the
  next call without a new login.
- Every endpoint carries `[HasPermission(Permissions.X)]`, except the auth flow and `/health`. Keys are constants in
  `Business/Access/Permissions.cs`.

---

## 5. Protection rules (`Business/Users/UserAccessRules`, `Business/Roles/RoleService`)

| Rule | Refusal |
|---|---|
| A company user only sees users / roles / supplier codes of their company | `404` (does not admit the record exists) |
| Changing an **admin user** (holder of an `IS_ADMIN_ROLE` or Admin role), giving or editing an **admin role**, or giving / removing an **ADMIN-scope** right needs `user.manageadmin` | `403 ADMIN_USER_PROTECTED` |
| **SYSTEM-scope** rights (supplier code / liquor category add & edit) are never put in a role or given to a user | `403 RIGHT_NOT_GRANTABLE` |
| Nobody but Admin changes their own roles or rights | `409 CANNOT_CHANGE_OWN_ACCESS` |
| Admin role is given only by Admin, to a user without a company | `403` / `400` |
| A role's supplier code is set on create and never changes; an admin role has none | `400` |
| A role of a deactivated supplier code cannot be given | `404` |
| Admin role cannot change; templates are changed by Admin only | `409 ROLE_NOT_EDITABLE` |
| A role still assigned to users, or inside a role group, cannot be deleted | `409 ROLE_IN_USE` |
| A role group given to any user cannot be deleted | `409 ROLE_GROUP_IN_USE` |
| A group with an admin role, or held by an admin user, is changed / given only with `user.manageadmin`; nobody but Admin changes a group they hold | `403 ADMIN_USER_PROTECTED` / `409 CANNOT_CHANGE_OWN_ACCESS` |
| A user keeps at least one role or one role group | `400` |
| Every role of a user needs a password policy | `409 PASSWORD_POLICY_NOT_CONFIGURED` |

Only the **changes** in a rights list are checked for grant scope, so saving an unchanged grid never fails on rights the editor could
not give.

---

## 6. Default roles and rights

| Role | Default rights |
|---|---|
| **Admin** (`IS_SYSTEM`) | Everything, in every company (no rows) |
| **Plant Admin** (`IS_ADMIN_ROLE`) | Every right except SYSTEM ones, incl. `user.manageadmin`, `securityconfig.*`, `passwordpolicy.*` |
| **Agent Manager** | `user.view/add/edit/status/unlock/access`, `role.view/add/edit/delete`, `suppliercode.view`, `liquorcategory.view` |
| **Plant Manager** | `user.view`, `role.view`, `suppliercode.view`, `liquorcategory.view` |
| **Supervisor / Operator / Viewer** | `suppliercode.view`, `liquorcategory.view`; production pages add their rights when they are built |

Permission keys today: `user.view/add/edit/status/unlock/access/manageadmin(ADMIN)`, `role.view/add/edit/delete`,
`securityconfig.view/edit (ADMIN)`, `passwordpolicy.view/edit (ADMIN)`, `suppliercode.view`, `suppliercode.add/edit (SYSTEM)`,
`liquorcategory.view`, `liquorcategory.add/edit (SYSTEM)`, `company.view (SYSTEM, script 010)`.

---

## 7. APIs

All responses have a body; errors are ProblemDetails with `errorCode` (see `05-user-module-api.md`).

| Method & path | Right | What it does |
|---|---|---|
| `POST /api/auth/login` | – | Also returns `supplierCodes[]` for the supplier code screen; `activeSupplierCode` is always null (nothing picked at login) |
| `GET /api/auth/mysuppliercodes` | logged in | Supplier codes the user may pick |
| `POST /api/auth/selectsuppliercode` `{ supplierCodeId }` | logged in | Picks / switches the supplier code; returns `MyPermissionsResponse`. Not the user's supplier code → `403 SUPPLIER_CODE_NOT_ASSIGNED` |
| `GET /api/auth/mypermissions` | logged in | `{ isSuperAdmin, activeSupplierCode, permissions[] }` for the menu and buttons |
| `GET /api/roles` · `GET /api/roles/{id}` | `role.view` | Roles of the company with `displayName` ("Operator RJ CL 772"); `?supplierCodeId=` → that supplier code's roles + company-level (Admin without a supplier code: the Admin role + templates) |
| `POST /api/roles` · `PUT /api/roles/{id}` | `role.add` / `role.edit` | `{ roleName, supplierCodeId, description, isAdminRole, perSupplierCode, passwordPolicyId }`; `supplierCodeId` is fixed after create |
| `DELETE /api/roles/{id}` | `role.delete` | Only when no user holds it |
| `GET /api/roles/{id}/rights` | `role.view` | The grid: every page and action with `granted`; `?applicationType=WEB` or `LINE` → one application |
| `PUT /api/roles/{id}/rights` `{ pageActionIds: [], applicationType? }` | `role.edit` | The **full** list of ticked actions (with `applicationType`: of that application only; the other one is kept) |
| `GET /api/pages` | `role.view` | Every page with its actions and `applicationType` (empty grid); `?applicationType=WEB` or `LINE` |
| `POST /api/users` | `user.add` | Takes `roles: [{ roleId }]` (at least one; the supplier code comes with the role); `companyId` is used only for Admin |
| `GET /api/users/{id}/access` | `user.view` | The user's roles and custom rights, with supplier code names |
| `PUT /api/users/{id}/roles` `{ roles: [] }` | `user.access` | The **full** list of direct roles (may be empty while the user holds a group) |
| `PUT /api/users/{id}/rolegroups` `{ roleGroupIds: [] }` | `user.access` | The **full** list of role groups |
| `GET` · `POST` · `PUT` · `DELETE` `/api/rolegroups[/{id}]`, `PUT /api/rolegroups/{id}/roles` `{ roleIds: [] }` | `role.view` / `add` / `edit` / `delete` | Role groups of the company and the roles inside each |
| `PUT /api/users/{id}/rights` `{ rights: [{ pageActionId, supplierCodeId }] }` | `user.access` | The **full** list of custom rights |
| `GET /api/suppliercodes` · `GET /api/suppliercodes/{id}` | `suppliercode.view` | Supplier codes of the company (Admin: all) |
| `POST /api/suppliercodes` · `PUT /api/suppliercodes/{id}` · `POST …/activate` · `POST …/deactivate` | `suppliercode.add` / `edit` (Admin) | A new supplier code gets its default roles (Plant Manager / Supervisor / Operator / Viewer of that code); the first one of a company also the company-level ones |
| `GET /api/liquorcategories` · `GET /api/liquorcategories/{id}` | `liquorcategory.view` | |
| `GET /api/excises` | `suppliercode.view` | Every state excise (`id`, `exciseCode`, `exciseName`, `isActive`) for dropdowns |
| `GET /api/companies` | `company.view` (Admin) | Companies with `supplierCodeCount` (script `010`) |
| `POST` · `PUT` · `…/activate` · `…/deactivate` on `/api/liquorcategories` | `liquorcategory.add` / `edit` (Admin) | |

Changed for the frontend: `UserResponse` and `CurrentUserResponse` no longer have `roleId`; `UpdateUserRequest` is profile only
(`fullName`, `email`, `phone`, `employeeCode`).

---

## 8. Code map

| Layer | Files |
|---|---|
| Domain | `Entities/ROLES, ROLE_RIGHTS, USER_ROLES, USER_RIGHTS, PAGES, PAGE_ACTIONS, SUPPLIER_CODE, LIQUOR_CATEGORY, EXCISE`, `Rules/GrantScope`, `PASSWORD_POLICY.Strictest` |
| Business | `Access/` (CurrentAccess, Permissions, AccessService, SupplierCodeDirectory), `Roles/` (RoleService, RoleTemplates), `Users/` (UserAccessService, UserAccessRules), `SupplierCodes/`, `LiquorCategories/` |
| Infrastructure | `Repositories/AccessRepository, RoleRepository, UserAccessRepository, SupplierCodeRepository, LiquorCategoryRepository, SupplierCodeQuery` + configurations |
| Api | `Security/HasPermissionAttribute, PermissionAuthorization, SessionScope, TenantContext`; controllers Roles, Pages, SupplierCodes, LiquorCategories, Users (access), Auth |
| Tests | Domain (ROLES, strictest policy), Business (Access, Roles, UserAccess, Users), Infrastructure (AccessRepository on MySQL), Api (RolesRightsFlowTests end to end) |

---

## 9. Adding a page later (batch, plan …)

1. In the page's SQL script, insert its `PAGES` row (with `APPLICATION_TYPE` `WEB` or `LINE`) and its `PAGE_ACTIONS` (for batch e.g. `view, add, edit, delete, submit, approve,
   cancel, approvecancel`) with the right `GRANT_SCOPE`, and give the default templates their rights (e.g. Operator → submit,
   Supervisor → approve, Plant Manager → approvecancel).
2. Add the keys to `Business/Access/Permissions.cs` and put `[HasPermission(...)]` on each endpoint.
3. Nothing else changes: the rights grid, user access, `mypermissions` and the checks pick the new actions up.

The batch approval design discussed on 2026-10-07 (maker-checker configurable in `SECURITY_CONFIG`, cancel of an approved batch needs the
higher role's approval and is blocked once cases exist, generic approval history, pending list) is kept for that page; it is **not built yet**.
