-- ============================================================================
-- ST.LiquorTNT - security question on first login.
-- Run ONCE, after 010_company_view.sql.
--
-- Login marks the session SECURITY_QUESTION_PENDING = 1 when the user has not chosen a security question yet
-- (and SECURITY_QUESTION_ENABLED = 1). While it is set, the session may only reach the security-question screen
-- (PUT /api/securityquestions/mine), GET /api/auth/me and logout; every other call is 403 SECURITY_QUESTION_REQUIRED.
-- Setting the question clears the flag on all of the user's active sessions.
-- ============================================================================

ALTER TABLE USER_SESSION
    ADD COLUMN SECURITY_QUESTION_PENDING TINYINT(1) NOT NULL DEFAULT 0 AFTER ACTIVE_SUPPLIER_CODE_ID;
