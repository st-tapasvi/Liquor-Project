-- ============================================================================
-- ST.LiquorTNT - first administrator of a NEW (customer) installation.
-- Run ONCE on a new installation, after all numbered scripts. NEVER run on the dev database: the team and the
-- automatic tests log in there as admin / Admin@123 without the first-login steps.
--
-- Leaves the user 'admin' (system role Admin) with the temporary password Admin@123 and makes the first login go through:
--   1. login with Admin@123          -> 403 PASSWORD_CHANGE_REQUIRED
--   2. POST /api/auth/changepassword -> own password
--   3. login with the new password    -> securityQuestionRequired = true
--   4. PUT /api/securityquestions/mine -> then the app opens
-- ============================================================================

SET @admin = (SELECT ID FROM USERS WHERE USERNAME = 'admin');

UPDATE USERS
SET PASSWORD_HASH         = 'PBKDF2.SHA256.100000.sITHVTTZCqWgXp7n3jThJg==.oInLFS7drPbj2vs17CP9BWW480w57m7WGJb6013Bmb0=',  -- Admin@123
    FORCE_PASSWORD_CHANGE = 1,
    IS_ACTIVE             = 1,
    IS_BLOCKED            = 0,
    FAILED_LOGIN_ATTEMPTS = 0,
    LAST_FAILED_LOGIN_AT  = NULL,
    LOCKED_UNTIL          = NULL,
    PASSWORD_CHANGED_AT   = NOW(),
    PASSWORD_EXPIRES_AT   = NULL
WHERE ID = @admin;

-- no security question yet: the first login asks for one
DELETE FROM USER_SECURITY_QUESTION WHERE USER_ID = @admin;

-- no open session or reset request from setup / testing
UPDATE USER_SESSION SET STATUS = 'REVOKED', LOGOUT_AT = NOW() WHERE USER_ID = @admin AND STATUS = 'ACTIVE';
DELETE FROM PASSWORD_RESET_REQUEST WHERE USER_ID = @admin;
