-- ============================================================================
-- ST.LiquorTNT - DEMO DATA for development and frontend work. NEVER run on a customer installation.
-- Run after 012_supplier_code_roles.sql. Safe to re-run: every insert is skipped when the row already exists.
--
-- Company   : Globus Spirits Ltd
-- Supplier  : RJ 772 (CL), RJ 1028 (IMFL), JK 369 (IMFL)
-- Roles     : Plant Admin + Agent Manager (company-level) and, for EACH supplier code, Plant Manager / Supervisor /
--             Operator / Viewer ("Operator RJ CL 772"), with the rights and password policy of their template
-- Users     : one per role (below). Password of every demo user: Admin@123
--
--   globus.admin     Plant Admin    all supplier codes
--   globus.agent     Agent Manager  all supplier codes
--   globus.pm        Plant Manager  of all 3 supplier codes
--   rj.supervisor    Supervisor RJ CL 772 + Supervisor RJ IMFL 1028
--   rj772.operator   Operator RJ CL 772   (+ custom right user.view on RJ 772, as an example)
--   rj1028.operator  Operator RJ IMFL 1028
--   jk369.operator   Operator JK IMFL 369
--   globus.viewer    Viewer of all 3 supplier codes
-- ============================================================================

-- ---------------------------------------------------------------------------- company
INSERT INTO COMPANY (COMPANY_NAME, ALIAS_NAME, CONTACT_NO, ADDRESS, CITY, PINCODE, COUNTRY, EMAIL, IS_ACTIVE, CREATED_BY, CREATED_AT)
SELECT 'Globus Spirits Ltd', 'Globus Spirits', '9000000772', 'Plant Road, Industrial Area', 'Behror', '301701', 'India',
       'demo@globus-spirits.example', 1, 1, NOW()
WHERE NOT EXISTS (SELECT 1 FROM COMPANY WHERE COMPANY_NAME = 'Globus Spirits Ltd');

SET @company = (SELECT ID FROM COMPANY WHERE COMPANY_NAME = 'Globus Spirits Ltd');
SET @rj      = (SELECT EXCISE_ID FROM EXCISE WHERE EXCISE_CODE = 'RJ');
SET @jk      = (SELECT EXCISE_ID FROM EXCISE WHERE EXCISE_CODE = 'JK');
SET @cl      = (SELECT ID FROM LIQUOR_CATEGORY WHERE CATEGORY_CODE = 'CL');
SET @imfl    = (SELECT ID FROM LIQUOR_CATEGORY WHERE CATEGORY_CODE = 'IMFL');

-- ---------------------------------------------------------------------------- supplier codes
INSERT IGNORE INTO SUPPLIER_CODE (COMPANY_ID, FRANCHISE_NAME, EXCISE_ID, SUPPLIER_CODE, LIQUOR_CATEGORY_ID, IS_ACTIVE, CREATED_BY, CREATED_AT) VALUES
    (@company, NULL, @rj, '772',  @cl,   1, 1, NOW()),
    (@company, NULL, @rj, '1028', @imfl, 1, 1, NOW()),
    (@company, NULL, @jk, '369',  @imfl, 1, 1, NOW());

SET @rj772  = (SELECT ID FROM SUPPLIER_CODE WHERE EXCISE_ID = @rj AND SUPPLIER_CODE = '772');
SET @rj1028 = (SELECT ID FROM SUPPLIER_CODE WHERE EXCISE_ID = @rj AND SUPPLIER_CODE = '1028');
SET @jk369  = (SELECT ID FROM SUPPLIER_CODE WHERE EXCISE_ID = @jk AND SUPPLIER_CODE = '369');

-- ---------------------------------------------------------------------------- default roles (same as RoleTemplates in code)
-- company-level templates (Plant Admin, Agent Manager) once for the company;
-- per-supplier-code templates (Plant Manager, Supervisor, Operator, Viewer) for every supplier code -> "Operator RJ CL 772"
INSERT IGNORE INTO ROLES (COMPANY_ID, SUPPLIER_CODE_ID, ROLE_NAME, DESCRIPTION, IS_SYSTEM, IS_TEMPLATE, IS_ADMIN_ROLE, IS_ACTIVE, CREATED_BY, CREATED_AT)
SELECT @company, NULL, t.ROLE_NAME, t.DESCRIPTION, 0, 0, t.IS_ADMIN_ROLE, 1, 1, NOW()
FROM ROLES t
WHERE t.IS_TEMPLATE = 1 AND t.PER_SUPPLIER_CODE = 0;

INSERT IGNORE INTO ROLES (COMPANY_ID, SUPPLIER_CODE_ID, ROLE_NAME, DESCRIPTION, IS_SYSTEM, IS_TEMPLATE, IS_ADMIN_ROLE, IS_ACTIVE, CREATED_BY, CREATED_AT)
SELECT @company, s.ID, t.ROLE_NAME, t.DESCRIPTION, 0, 0, 0, 1, 1, NOW()
FROM ROLES t
JOIN SUPPLIER_CODE s ON s.COMPANY_ID = @company
WHERE t.IS_TEMPLATE = 1 AND t.PER_SUPPLIER_CODE = 1;

-- every copy gets the rights and password policy of its template
INSERT IGNORE INTO ROLE_RIGHTS (ROLE_ID, PAGE_ACTION_ID, CREATED_BY, CREATED_AT)
SELECT c.ID, rr.PAGE_ACTION_ID, 1, NOW()
FROM ROLES t
JOIN ROLES c        ON c.COMPANY_ID = @company AND c.ROLE_NAME = t.ROLE_NAME
JOIN ROLE_RIGHTS rr ON rr.ROLE_ID = t.ID
WHERE t.IS_TEMPLATE = 1;

INSERT IGNORE INTO ROLE_PASSWORD_POLICY (ROLE_ID, PASSWORD_POLICY_ID, CREATED_BY, CREATED_AT, UPDATED_BY, UPDATED_AT)
SELECT c.ID, p.PASSWORD_POLICY_ID, 1, NOW(), 1, NOW()
FROM ROLES t
JOIN ROLES c                 ON c.COMPANY_ID = @company AND c.ROLE_NAME = t.ROLE_NAME
JOIN ROLE_PASSWORD_POLICY p  ON p.ROLE_ID = t.ID
WHERE t.IS_TEMPLATE = 1;

-- ---------------------------------------------------------------------------- users (password Admin@123, PBKDF2 hash of the seed admin)
SET @hash = 'PBKDF2.SHA256.100000.sITHVTTZCqWgXp7n3jThJg==.oInLFS7drPbj2vs17CP9BWW480w57m7WGJb6013Bmb0=';

INSERT IGNORE INTO USERS (USERNAME, FULL_NAME, COMPANY_ID, PASSWORD_HASH, EMAIL, PHONE, EMPLOYEE_CODE,
                          FORCE_PASSWORD_CHANGE, IS_ACTIVE, PASSWORD_CHANGED_AT, CREATED_BY, CREATED_AT) VALUES
    ('globus.admin',    'Rajesh Sharma',  @company, @hash, 'rajesh.sharma@globus-spirits.example',  '9000000101', 'GSL-101', 0, 1, NOW(), 1, NOW()),
    ('globus.agent',    'Neha Agarwal',   @company, @hash, 'neha.agarwal@globus-spirits.example',   '9000000102', 'GSL-102', 0, 1, NOW(), 1, NOW()),
    ('globus.pm',       'Vikram Singh',   @company, @hash, 'vikram.singh@globus-spirits.example',   '9000000103', 'GSL-103', 0, 1, NOW(), 1, NOW()),
    ('rj.supervisor',   'Suresh Yadav',   @company, @hash, 'suresh.yadav@globus-spirits.example',   '9000000104', 'GSL-104', 0, 1, NOW(), 1, NOW()),
    ('rj772.operator',  'Mahesh Meena',   @company, @hash, 'mahesh.meena@globus-spirits.example',   '9000000105', 'GSL-105', 0, 1, NOW(), 1, NOW()),
    ('rj1028.operator', 'Ramesh Gurjar',  @company, @hash, 'ramesh.gurjar@globus-spirits.example',  '9000000106', 'GSL-106', 0, 1, NOW(), 1, NOW()),
    ('jk369.operator',  'Anil Mahto',     @company, @hash, 'anil.mahto@globus-spirits.example',     '9000000107', 'GSL-107', 0, 1, NOW(), 1, NOW()),
    ('globus.viewer',   'Pooja Verma',    @company, @hash, 'pooja.verma@globus-spirits.example',    '9000000108', 'GSL-108', 0, 1, NOW(), 1, NOW());

-- password history row, as the application writes one with every password
INSERT INTO USER_PASSWORD_HISTORY (USER_ID, PASSWORD_HASH, CREATED_AT)
SELECT u.ID, u.PASSWORD_HASH, NOW()
FROM USERS u
WHERE u.COMPANY_ID = @company
  AND NOT EXISTS (SELECT 1 FROM USER_PASSWORD_HISTORY h WHERE h.USER_ID = u.ID);

-- ---------------------------------------------------------------------------- role assignments
-- A role carries its supplier code, so "Supervisor" on RJ 772 is the role "Supervisor" of supplier code RJ 772.
-- SUPPLIER_CODE_ID NULL below = the company-level role; 'ALL' = the role of that name in every supplier code.
INSERT IGNORE INTO USER_ROLES (USER_ID, ROLE_ID, CREATED_BY, CREATED_AT)
SELECT u.ID, r.ID, 1, NOW()
FROM (
              SELECT 'globus.admin'    USERNAME, 'Plant Admin'   ROLE_NAME, NULL   SUPPLIER_CODE_ID, 0 ALL_CODES
    UNION ALL SELECT 'globus.agent',             'Agent Manager',           NULL,                     0
    UNION ALL SELECT 'globus.pm',                'Plant Manager',           NULL,                     1
    UNION ALL SELECT 'rj.supervisor',            'Supervisor',              @rj772,                   0
    UNION ALL SELECT 'rj.supervisor',            'Supervisor',              @rj1028,                  0
    UNION ALL SELECT 'rj772.operator',           'Operator',                @rj772,                   0
    UNION ALL SELECT 'rj1028.operator',          'Operator',                @rj1028,                  0
    UNION ALL SELECT 'jk369.operator',           'Operator',                @jk369,                   0
    UNION ALL SELECT 'globus.viewer',            'Viewer',                  NULL,                     1
) a
JOIN USERS u ON u.USERNAME = a.USERNAME
JOIN ROLES r ON r.COMPANY_ID = @company
            AND r.ROLE_NAME = a.ROLE_NAME
            AND (a.ALL_CODES = 1 OR r.SUPPLIER_CODE_ID <=> a.SUPPLIER_CODE_ID);

-- one custom right, so the access screen has an example: rj772.operator may view users in RJ 772
INSERT INTO USER_RIGHTS (USER_ID, PAGE_ACTION_ID, SUPPLIER_CODE_ID, CREATED_BY, CREATED_AT)
SELECT u.ID, pa.ID, @rj772, 1, NOW()
FROM USERS u
JOIN PAGE_ACTIONS pa ON pa.PERMISSION_KEY = 'user.view'
WHERE u.USERNAME = 'rj772.operator'
  AND NOT EXISTS (SELECT 1 FROM USER_RIGHTS r WHERE r.USER_ID = u.ID AND r.PAGE_ACTION_ID = pa.ID AND r.SUPPLIER_CODE_ID <=> @rj772);
