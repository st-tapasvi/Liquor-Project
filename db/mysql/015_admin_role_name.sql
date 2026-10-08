-- ============================================================================
-- ST.LiquorTNT - the system role of the 'admin' user is called "Admin".
-- Run ONCE, after 014_security_questions.sql.
--
-- Owner, 2026-10-08: for now there is only the 'admin' user. The Super Admin of Sundaram Technologies is a separate,
-- later feature and must not be mixed in, so the system role (IS_SYSTEM = 1, every right in every company) is named
-- "Admin". Nothing else changes: the code finds this role by IS_SYSTEM, never by name.
-- ============================================================================

UPDATE ROLES
SET ROLE_NAME   = 'Admin',
    DESCRIPTION = 'Administrator of this installation - every right in every company'
WHERE IS_SYSTEM = 1;
