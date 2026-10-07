-- ============================================================================
-- ST.LiquorTNT - Roles & Rights and Supplier codes.
-- Design: docs/06-roles-rights-plan.md
-- Run ONCE, after 003-008. Seeds use INSERT IGNORE.
--
-- What this script does, in order:
--   1. LIQUOR_CATEGORY      new lookup table (IMFL / CL / FL seeded)
--   2. SUPPLIER_CODE        new table: one supplier code of a company (excise + code + category)
--   3. COMPANY              EXCISE_CODE and SUPPLIER_CODE move out (now in SUPPLIER_CODE)
--   4. PAGES / PAGE_ACTIONS every screen and the actions it supports (= permission keys)
--   5. ROLES                company-wise roles, Super Admin, default role templates
--   6. ROLE_RIGHTS          rebuilt: one row per (role, page action)
--   7. USER_ROLES / USER_RIGHTS  a user's roles (per supplier code) and custom rights
--   8. USERS.ROLE_ID        moved into USER_ROLES, then dropped
--   9. USER_SESSION         remembers the supplier code picked after login
--  10. SECURITY_CONFIG      ADMIN_ROLE_ID retired (rights now come from roles)
-- ============================================================================


-- ----------------------------------------------------------------------------
-- 1. LIQUOR_CATEGORY - CL / FL / IMFL ... The full list comes from the CRM later.
-- ----------------------------------------------------------------------------
CREATE TABLE LIQUOR_CATEGORY (
    ID             INT UNSIGNED NOT NULL AUTO_INCREMENT,
    CATEGORY_CODE  VARCHAR(20)  NOT NULL,
    CATEGORY_NAME  VARCHAR(100) NOT NULL,
    DESCRIPTION    VARCHAR(500) NULL,
    IS_ACTIVE      TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_BY     INT          NULL,
    CREATED_AT     DATETIME     NULL,
    UPDATED_BY     INT          NULL,
    UPDATED_AT     DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_LIQUOR_CATEGORY_CODE (CATEGORY_CODE)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO LIQUOR_CATEGORY (CATEGORY_CODE, CATEGORY_NAME, DESCRIPTION, CREATED_AT) VALUES
    ('IMFL', 'Indian Made Foreign Liquor', 'Spirits made in India in the style of foreign liquor: whisky, rum, brandy, gin, vodka.', NOW()),
    ('CL',   'Country Liquor',             'Liquor made in India other than IMFL and FL.', NOW()),
    ('FL',   'Foreign Liquor',             'Liquor imported into India from abroad.', NOW());


-- ----------------------------------------------------------------------------
-- 2. SUPPLIER_CODE - one supplier code of a company, e.g. Globus / RJ / 550 / CL.
--    A code never repeats inside one excise (owner decision 2026-10-07).
-- ----------------------------------------------------------------------------
CREATE TABLE SUPPLIER_CODE (
    ID                  INT UNSIGNED NOT NULL AUTO_INCREMENT,
    COMPANY_ID          INT UNSIGNED NOT NULL,
    FRANCHISE_NAME      VARCHAR(200) NULL,
    EXCISE_ID           INT UNSIGNED NOT NULL,
    SUPPLIER_CODE       VARCHAR(20)  NOT NULL,
    LIQUOR_CATEGORY_ID  INT UNSIGNED NOT NULL,
    IS_ACTIVE           TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_BY          INT          NULL,
    CREATED_AT          DATETIME     NULL,
    UPDATED_BY          INT          NULL,
    UPDATED_AT          DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_SUPPLIER_CODE_EXCISE (EXCISE_ID, SUPPLIER_CODE),
    KEY IDX_SUPPLIER_CODE_COMPANY (COMPANY_ID),
    CONSTRAINT FK_SUPPLIER_CODE_COMPANY  FOREIGN KEY (COMPANY_ID)         REFERENCES COMPANY (ID),
    CONSTRAINT FK_SUPPLIER_CODE_EXCISE   FOREIGN KEY (EXCISE_ID)          REFERENCES EXCISE (EXCISE_ID),
    CONSTRAINT FK_SUPPLIER_CODE_CATEGORY FOREIGN KEY (LIQUOR_CATEGORY_ID) REFERENCES LIQUOR_CATEGORY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ----------------------------------------------------------------------------
-- 3. COMPANY - excise and supplier code now live in SUPPLIER_CODE (the table is empty today).
-- ----------------------------------------------------------------------------
ALTER TABLE COMPANY
    DROP FOREIGN KEY FK_COMPANY_EXCISE,
    DROP INDEX UQ_COMPANY_EXCISE_NAME,
    DROP INDEX IDX_EXCISE_CODE,
    DROP COLUMN EXCISE_CODE,
    DROP COLUMN SUPPLIER_CODE,
    ADD UNIQUE KEY UQ_COMPANY_NAME (COMPANY_NAME);


-- ----------------------------------------------------------------------------
-- 4. PAGES and PAGE_ACTIONS
--    PAGE_ACTIONS.PERMISSION_KEY is what [HasPermission("user.add")] checks.
--    GRANT_SCOPE says who may hand the right to a role or a user:
--      ANY    - anyone who manages roles / user access
--      ADMIN  - only a holder of user.manageadmin (Plant Admin by default)
--      SYSTEM - nobody; only Super Admin has it (masters that come from the CRM)
-- ----------------------------------------------------------------------------
ALTER TABLE PAGES
    ADD COLUMN SORT_ORDER INT NOT NULL DEFAULT 0 AFTER PAGE_KEY;

INSERT IGNORE INTO PAGES (PAGE_NAME, MODULE_NAME, PAGE_KEY, SORT_ORDER) VALUES
    ('Role',             'Administration', 'role',            20),
    ('Security Config',  'Administration', 'securityconfig',  30),
    ('Password Policy',  'Administration', 'passwordpolicy',  40),
    ('Supplier Code',    'Masters',        'suppliercode',    50),
    ('Liquor Category',  'Masters',        'liquorcategory',  60);

UPDATE PAGES SET MODULE_NAME = 'Administration', SORT_ORDER = 10 WHERE PAGE_KEY = 'user';
UPDATE PAGES SET SORT_ORDER = 90  WHERE PAGE_KEY = 'company';
UPDATE PAGES SET SORT_ORDER = 100 WHERE PAGE_KEY = 'brand';

CREATE TABLE PAGE_ACTIONS (
    ID              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    PAGE_ID         INT UNSIGNED NOT NULL,
    ACTION_KEY      VARCHAR(30)  NOT NULL,
    PERMISSION_KEY  VARCHAR(80)  NOT NULL,
    ACTION_NAME     VARCHAR(100) NOT NULL,
    GRANT_SCOPE     VARCHAR(10)  NOT NULL DEFAULT 'ANY',
    SORT_ORDER      INT          NOT NULL DEFAULT 0,
    IS_ACTIVE       TINYINT(1)   NOT NULL DEFAULT 1,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_PAGE_ACTIONS_KEY (PERMISSION_KEY),
    UNIQUE KEY UQ_PAGE_ACTIONS_PAGE_ACTION (PAGE_ID, ACTION_KEY),
    CONSTRAINT FK_PAGE_ACTIONS_PAGE FOREIGN KEY (PAGE_ID) REFERENCES PAGES (ID),
    CONSTRAINT CK_PAGE_ACTIONS_SCOPE CHECK (GRANT_SCOPE IN ('ANY', 'ADMIN', 'SYSTEM'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO PAGE_ACTIONS (PAGE_ID, ACTION_KEY, PERMISSION_KEY, ACTION_NAME, GRANT_SCOPE, SORT_ORDER)
SELECT p.ID, a.ACTION_KEY, CONCAT(p.PAGE_KEY, '.', a.ACTION_KEY), a.ACTION_NAME, a.GRANT_SCOPE, a.SORT_ORDER
FROM PAGES p
JOIN (
              SELECT 'user' PAGE_KEY, 'view' ACTION_KEY, 'View' ACTION_NAME, 'ANY' GRANT_SCOPE, 1 SORT_ORDER
    UNION ALL SELECT 'user', 'add',          'Create',                    'ANY',    2
    UNION ALL SELECT 'user', 'edit',         'Edit',                      'ANY',    3
    UNION ALL SELECT 'user', 'status',       'Activate / deactivate',     'ANY',    4
    UNION ALL SELECT 'user', 'unlock',       'Unlock',                    'ANY',    5
    UNION ALL SELECT 'user', 'access',       'Assign roles and rights',   'ANY',    6
    UNION ALL SELECT 'user', 'manageadmin',  'Manage admin users',        'ADMIN',  7
    UNION ALL SELECT 'role', 'view',         'View',                      'ANY',    1
    UNION ALL SELECT 'role', 'add',          'Create',                    'ANY',    2
    UNION ALL SELECT 'role', 'edit',         'Edit (name and rights)',    'ANY',    3
    UNION ALL SELECT 'role', 'delete',       'Delete',                    'ANY',    4
    UNION ALL SELECT 'securityconfig', 'view', 'View',                    'ADMIN',  1
    UNION ALL SELECT 'securityconfig', 'edit', 'Edit',                    'ADMIN',  2
    UNION ALL SELECT 'passwordpolicy', 'view', 'View',                    'ADMIN',  1
    UNION ALL SELECT 'passwordpolicy', 'edit', 'Edit',                    'ADMIN',  2
    UNION ALL SELECT 'suppliercode', 'view', 'View',                      'ANY',    1
    UNION ALL SELECT 'suppliercode', 'add',  'Create',                    'SYSTEM', 2
    UNION ALL SELECT 'suppliercode', 'edit', 'Edit / activate',           'SYSTEM', 3
    UNION ALL SELECT 'liquorcategory', 'view', 'View',                    'ANY',    1
    UNION ALL SELECT 'liquorcategory', 'add',  'Create',                  'SYSTEM', 2
    UNION ALL SELECT 'liquorcategory', 'edit', 'Edit / activate',         'SYSTEM', 3
) a ON a.PAGE_KEY = p.PAGE_KEY;


-- ----------------------------------------------------------------------------
-- 5. ROLES - company-wise.
--    COMPANY_ID NULL  = Super Admin (IS_SYSTEM) or a default template (IS_TEMPLATE).
--    IS_ADMIN_ROLE    = users holding it are "admin users" (Plant Admin): only a holder of
--                       user.manageadmin may create, edit or assign them.
--    The role name is unique inside one company.
-- ----------------------------------------------------------------------------
ALTER TABLE ROLES
    ADD COLUMN COMPANY_ID    INT UNSIGNED NULL       AFTER ID,
    ADD COLUMN IS_SYSTEM     TINYINT(1)   NOT NULL DEFAULT 0 AFTER DESCRIPTION,
    ADD COLUMN IS_TEMPLATE   TINYINT(1)   NOT NULL DEFAULT 0 AFTER IS_SYSTEM,
    ADD COLUMN IS_ADMIN_ROLE TINYINT(1)   NOT NULL DEFAULT 0 AFTER IS_TEMPLATE,
    DROP INDEX UNIQ_ROLE_NAME,
    ADD UNIQUE KEY UQ_ROLES_COMPANY_NAME (COMPANY_ID, ROLE_NAME),
    ADD CONSTRAINT FK_ROLES_COMPANY FOREIGN KEY (COMPANY_ID) REFERENCES COMPANY (ID);

-- The existing Administrator (ID 1) becomes Sundaram Tech's Super Admin.
UPDATE ROLES
SET ROLE_NAME = 'Super Admin', DESCRIPTION = 'Sundaram Tech - every right in every company', IS_SYSTEM = 1
WHERE ID = 1;

-- Default role templates: copied into a company when its first supplier code is created.
INSERT IGNORE INTO ROLES (COMPANY_ID, ROLE_NAME, DESCRIPTION, IS_TEMPLATE, IS_ADMIN_ROLE) VALUES
    (NULL, 'Plant Admin',   'Head of the plant: every right of the company',        1, 1),
    (NULL, 'Agent Manager', 'Creates users, roles and rights (not admin users)',    1, 0),
    (NULL, 'Plant Manager', 'Runs the plant; views users and masters',             1, 0),
    (NULL, 'Supervisor',    'Supervises production (rights added with each page)',  1, 0),
    (NULL, 'Operator',      'Works on the line (rights added with each page)',      1, 0),
    (NULL, 'Viewer',        'Read only',                                            1, 0);

-- Every role needs a password policy (user creation refuses a role without one).
INSERT IGNORE INTO ROLE_PASSWORD_POLICY (ROLE_ID, PASSWORD_POLICY_ID, CREATED_AT, UPDATED_AT)
SELECT r.ID, (SELECT ID FROM PASSWORD_POLICY WHERE POLICY_NAME = 'MEDIUM'), NOW(), NOW()
FROM ROLES r
WHERE r.IS_TEMPLATE = 1;


-- ----------------------------------------------------------------------------
-- 6. ROLE_RIGHTS - rebuilt. One row = this role has this page action.
--    The old CAN_VIEW..CAN_EXPORT rows only belonged to Administrator, which is now Super Admin
--    and has every right without rows, so nothing needs converting.
-- ----------------------------------------------------------------------------
DROP TABLE ROLE_RIGHTS;

CREATE TABLE ROLE_RIGHTS (
    ID              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ROLE_ID         INT UNSIGNED NOT NULL,
    PAGE_ACTION_ID  INT UNSIGNED NOT NULL,
    CREATED_BY      INT          NULL,
    CREATED_AT      DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_ROLE_RIGHTS (ROLE_ID, PAGE_ACTION_ID),
    KEY IDX_ROLE_RIGHTS_ACTION (PAGE_ACTION_ID),
    CONSTRAINT FK_ROLE_RIGHTS_ROLE   FOREIGN KEY (ROLE_ID)        REFERENCES ROLES (ID) ON DELETE CASCADE,
    CONSTRAINT FK_ROLE_RIGHTS_ACTION FOREIGN KEY (PAGE_ACTION_ID) REFERENCES PAGE_ACTIONS (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Default rights of the templates (docs/06 section 8). SYSTEM rights are never granted.
INSERT IGNORE INTO ROLE_RIGHTS (ROLE_ID, PAGE_ACTION_ID, CREATED_AT)
SELECT r.ID, pa.ID, NOW()
FROM ROLES r
JOIN PAGE_ACTIONS pa ON pa.GRANT_SCOPE <> 'SYSTEM'
WHERE r.IS_TEMPLATE = 1
  AND (
        r.ROLE_NAME = 'Plant Admin'
     OR (r.ROLE_NAME = 'Agent Manager' AND pa.PERMISSION_KEY IN (
            'user.view', 'user.add', 'user.edit', 'user.status', 'user.unlock', 'user.access',
            'role.view', 'role.add', 'role.edit', 'role.delete',
            'suppliercode.view', 'liquorcategory.view'))
     OR (r.ROLE_NAME = 'Plant Manager' AND pa.PERMISSION_KEY IN (
            'user.view', 'role.view', 'suppliercode.view', 'liquorcategory.view'))
     OR (r.ROLE_NAME IN ('Supervisor', 'Operator', 'Viewer') AND pa.PERMISSION_KEY IN (
            'suppliercode.view', 'liquorcategory.view'))
  );
-- Production pages (batch, plan ...) add their rights to these templates in their own scripts.


-- ----------------------------------------------------------------------------
-- 7. USER_ROLES / USER_RIGHTS
--    SUPPLIER_CODE_ID NULL = every supplier code of the user's company (Plant Admin, Agent Manager)
--    or, for Super Admin, no company at all.
--    USER_RIGHTS only ADD rights (custom grants); they never take a role's right away.
-- ----------------------------------------------------------------------------
CREATE TABLE USER_ROLES (
    ID                INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID           INT UNSIGNED NOT NULL,
    ROLE_ID           INT UNSIGNED NOT NULL,
    SUPPLIER_CODE_ID  INT UNSIGNED NULL,
    CREATED_BY        INT          NULL,
    CREATED_AT        DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_USER_ROLES (USER_ID, ROLE_ID, SUPPLIER_CODE_ID),
    KEY IDX_USER_ROLES_ROLE (ROLE_ID),
    CONSTRAINT FK_USER_ROLES_USER    FOREIGN KEY (USER_ID)          REFERENCES USERS (ID) ON DELETE CASCADE,
    CONSTRAINT FK_USER_ROLES_ROLE    FOREIGN KEY (ROLE_ID)          REFERENCES ROLES (ID),
    CONSTRAINT FK_USER_ROLES_SUPPLIER_CODE FOREIGN KEY (SUPPLIER_CODE_ID) REFERENCES SUPPLIER_CODE (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE USER_RIGHTS (
    ID                INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID           INT UNSIGNED NOT NULL,
    PAGE_ACTION_ID    INT UNSIGNED NOT NULL,
    SUPPLIER_CODE_ID  INT UNSIGNED NULL,
    CREATED_BY        INT          NULL,
    CREATED_AT        DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_USER_RIGHTS (USER_ID, PAGE_ACTION_ID, SUPPLIER_CODE_ID),
    CONSTRAINT FK_USER_RIGHTS_USER    FOREIGN KEY (USER_ID)          REFERENCES USERS (ID) ON DELETE CASCADE,
    CONSTRAINT FK_USER_RIGHTS_ACTION  FOREIGN KEY (PAGE_ACTION_ID)   REFERENCES PAGE_ACTIONS (ID),
    CONSTRAINT FK_USER_RIGHTS_SUPPLIER_CODE FOREIGN KEY (SUPPLIER_CODE_ID) REFERENCES SUPPLIER_CODE (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ----------------------------------------------------------------------------
-- 8. USERS.ROLE_ID -> USER_ROLES, then the column goes.
-- ----------------------------------------------------------------------------
INSERT IGNORE INTO USER_ROLES (USER_ID, ROLE_ID, SUPPLIER_CODE_ID, CREATED_AT)
SELECT ID, ROLE_ID, NULL, NOW() FROM USERS WHERE ROLE_ID IS NOT NULL;

ALTER TABLE USERS
    DROP FOREIGN KEY FK_USERS_ROLE_ID,
    DROP INDEX IDX_ROLE_ID,
    DROP COLUMN ROLE_ID;


-- ----------------------------------------------------------------------------
-- 9. USER_SESSION - the supplier code the user is working in (picked after login, switchable).
-- ----------------------------------------------------------------------------
ALTER TABLE USER_SESSION
    ADD COLUMN ACTIVE_SUPPLIER_CODE_ID INT UNSIGNED NULL AFTER USER_ID,
    ADD CONSTRAINT FK_SESSION_SUPPLIER_CODE FOREIGN KEY (ACTIVE_SUPPLIER_CODE_ID) REFERENCES SUPPLIER_CODE (ID);


-- ----------------------------------------------------------------------------
-- 10. SECURITY_CONFIG - ADMIN_ROLE_ID is retired: rights now come from roles.
-- ----------------------------------------------------------------------------
DELETE FROM SECURITY_CONFIG WHERE CONFIG_KEY = 'ADMIN_ROLE_ID';
