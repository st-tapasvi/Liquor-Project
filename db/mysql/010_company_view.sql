-- ============================================================================
-- ST.LiquorTNT - permission for the company list (GET /api/companies).
-- Run ONCE, after 009_roles_rights.sql.
--
-- company.view is SYSTEM scope: companies come from the CRM and only Super Admin works across them,
-- so no company role can hold this right. (The excise list, GET /api/excises, uses suppliercode.view.)
-- ============================================================================

INSERT IGNORE INTO PAGE_ACTIONS (PAGE_ID, ACTION_KEY, PERMISSION_KEY, ACTION_NAME, GRANT_SCOPE, SORT_ORDER)
SELECT p.ID, 'view', 'company.view', 'View', 'SYSTEM', 1
FROM PAGES p
WHERE p.PAGE_KEY = 'company';

UPDATE PAGES SET MODULE_NAME = 'Masters' WHERE PAGE_KEY = 'company';
