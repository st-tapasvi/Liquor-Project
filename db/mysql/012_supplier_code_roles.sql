-- ============================================================================
-- ST.LiquorTNT - master roles per supplier code.
-- Run ONCE, after 011_security_question_first_login.sql. Take a backup first.
--
-- Before: a role was company-wide ("Operator") and USER_ROLES said for which supplier code the user held it.
-- After : the role itself belongs to a supplier code ("Operator" of RJ CL 772, shown as "Operator RJ CL 772"),
--         so each supplier code can have its own rights. A role with SUPPLIER_CODE_ID NULL is a company-level
--         role (Plant Admin, Agent Manager) and covers every supplier code of the company.
--         USER_ROLES no longer has a supplier code: it comes from the role.
--
-- Default templates: PER_SUPPLIER_CODE = 1 (Plant Manager, Supervisor, Operator, Viewer) are copied for every new
-- supplier code; PER_SUPPLIER_CODE = 0 (Plant Admin, Agent Manager) once per company.
--
-- Existing data is converted without changing what anybody may do:
--   * every company role of the per-supplier-code kind is split into one role per supplier code of its company,
--     with the same rights and password policy;
--   * a user who held it for one supplier code gets that supplier code's role; for "all supplier codes", every copy;
--   * the old company-wide role is removed.
-- ============================================================================

-- ---------------------------------------------------------------------------- 1. ROLES: supplier code + template kind
-- SUPPLIER_CODE_KEY (0 for company-level roles) only exists so the unique key also works when SUPPLIER_CODE_ID is NULL.
ALTER TABLE ROLES
    ADD COLUMN SUPPLIER_CODE_ID INT UNSIGNED NULL AFTER COMPANY_ID,
    ADD COLUMN PER_SUPPLIER_CODE TINYINT(1) NOT NULL DEFAULT 0 AFTER IS_ADMIN_ROLE,
    ADD COLUMN SUPPLIER_CODE_KEY INT UNSIGNED AS (IFNULL(SUPPLIER_CODE_ID, 0)) STORED,
    ADD CONSTRAINT FK_ROLES_SUPPLIER_CODE FOREIGN KEY (SUPPLIER_CODE_ID) REFERENCES SUPPLIER_CODE (ID);

ALTER TABLE ROLES
    ADD UNIQUE KEY UQ_ROLES_COMPANY_SUPPLIER_NAME (COMPANY_ID, SUPPLIER_CODE_KEY, ROLE_NAME),
    DROP INDEX UQ_ROLES_COMPANY_NAME;

UPDATE ROLES SET PER_SUPPLIER_CODE = 1
WHERE IS_TEMPLATE = 1 AND ROLE_NAME IN ('Plant Manager', 'Supervisor', 'Operator', 'Viewer');

-- ---------------------------------------------------------------------------- 2. split company roles per supplier code
DROP TEMPORARY TABLE IF EXISTS TMP_SPLIT;
CREATE TEMPORARY TABLE TMP_SPLIT (OLD_ROLE_ID INT UNSIGNED PRIMARY KEY);

-- the per-supplier-code kind: named like a per-supplier-code template, or already given for one supplier code
INSERT INTO TMP_SPLIT (OLD_ROLE_ID)
SELECT r.ID
FROM ROLES r
WHERE r.COMPANY_ID IS NOT NULL
  AND r.SUPPLIER_CODE_ID IS NULL
  AND r.IS_ADMIN_ROLE = 0
  AND (r.ROLE_NAME IN (SELECT t.ROLE_NAME FROM ROLES t WHERE t.IS_TEMPLATE = 1 AND t.PER_SUPPLIER_CODE = 1)
       OR EXISTS (SELECT 1 FROM USER_ROLES ur WHERE ur.ROLE_ID = r.ID AND ur.SUPPLIER_CODE_ID IS NOT NULL));

INSERT INTO ROLES (COMPANY_ID, SUPPLIER_CODE_ID, ROLE_NAME, DESCRIPTION, IS_SYSTEM, IS_TEMPLATE, IS_ADMIN_ROLE,
                   PER_SUPPLIER_CODE, IS_ACTIVE, CREATED_BY, CREATED_AT)
SELECT r.COMPANY_ID, s.ID, r.ROLE_NAME, r.DESCRIPTION, 0, 0, 0, 0, r.IS_ACTIVE, r.CREATED_BY, NOW()
FROM TMP_SPLIT x
JOIN ROLES r         ON r.ID = x.OLD_ROLE_ID
JOIN SUPPLIER_CODE s ON s.COMPANY_ID = r.COMPANY_ID;

DROP TEMPORARY TABLE IF EXISTS TMP_MAP;
CREATE TEMPORARY TABLE TMP_MAP AS
SELECT x.OLD_ROLE_ID, n.SUPPLIER_CODE_ID, n.ID AS NEW_ROLE_ID
FROM TMP_SPLIT x
JOIN ROLES o ON o.ID = x.OLD_ROLE_ID
JOIN ROLES n ON n.COMPANY_ID = o.COMPANY_ID AND n.ROLE_NAME = o.ROLE_NAME AND n.SUPPLIER_CODE_ID IS NOT NULL;

INSERT INTO ROLE_RIGHTS (ROLE_ID, PAGE_ACTION_ID, CREATED_BY, CREATED_AT)
SELECT m.NEW_ROLE_ID, rr.PAGE_ACTION_ID, rr.CREATED_BY, NOW()
FROM TMP_MAP m
JOIN ROLE_RIGHTS rr ON rr.ROLE_ID = m.OLD_ROLE_ID;

INSERT INTO ROLE_PASSWORD_POLICY (ROLE_ID, PASSWORD_POLICY_ID, CREATED_BY, CREATED_AT, UPDATED_BY, UPDATED_AT)
SELECT m.NEW_ROLE_ID, p.PASSWORD_POLICY_ID, p.CREATED_BY, NOW(), p.UPDATED_BY, NOW()
FROM TMP_MAP m
JOIN ROLE_PASSWORD_POLICY p ON p.ROLE_ID = m.OLD_ROLE_ID;

-- ---------------------------------------------------------------------------- 3. move the users onto the new roles
INSERT INTO USER_ROLES (USER_ID, ROLE_ID, SUPPLIER_CODE_ID, CREATED_BY, CREATED_AT)
SELECT ur.USER_ID, m.NEW_ROLE_ID, NULL, MIN(ur.CREATED_BY), MIN(ur.CREATED_AT)
FROM USER_ROLES ur
JOIN TMP_MAP m ON m.OLD_ROLE_ID = ur.ROLE_ID
              AND (ur.SUPPLIER_CODE_ID IS NULL OR ur.SUPPLIER_CODE_ID = m.SUPPLIER_CODE_ID)
GROUP BY ur.USER_ID, m.NEW_ROLE_ID;

DELETE ur FROM USER_ROLES ur JOIN TMP_SPLIT x ON x.OLD_ROLE_ID = ur.ROLE_ID;

DELETE FROM ROLE_PASSWORD_POLICY WHERE ROLE_ID IN (SELECT OLD_ROLE_ID FROM TMP_SPLIT);
DELETE FROM ROLE_RIGHTS          WHERE ROLE_ID IN (SELECT OLD_ROLE_ID FROM TMP_SPLIT);
DELETE FROM ROLES                WHERE ID      IN (SELECT OLD_ROLE_ID FROM TMP_SPLIT);

DROP TEMPORARY TABLE TMP_MAP;
DROP TEMPORARY TABLE TMP_SPLIT;

-- ---------------------------------------------------------------------------- 4. USER_ROLES: the supplier code comes from the role
-- What is left with a supplier code is a company-level role (it covers every supplier code anyway).
DELETE a FROM USER_ROLES a
JOIN USER_ROLES b ON a.USER_ID = b.USER_ID AND a.ROLE_ID = b.ROLE_ID AND a.ID > b.ID;

ALTER TABLE USER_ROLES DROP FOREIGN KEY FK_USER_ROLES_SUPPLIER_CODE;

ALTER TABLE USER_ROLES
    ADD UNIQUE KEY UQ_USER_ROLE (USER_ID, ROLE_ID),
    DROP INDEX UQ_USER_ROLES,
    DROP INDEX FK_USER_ROLES_SUPPLIER_CODE,
    DROP COLUMN SUPPLIER_CODE_ID;
