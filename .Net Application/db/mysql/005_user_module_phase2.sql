-- ============================================================================
-- ST.LiquorTNT - User Module: schema adjustments found while implementing the code.
-- Additive / relaxing only. Nothing is dropped.
-- ============================================================================

-- 1. USER_LOG: a log row must be writable when there is no user (failed login for an unknown
--    username) and USERS no longer carries an excise, so both columns become nullable.
ALTER TABLE USER_LOG
    MODIFY COLUMN USER_ID     INT          NULL,
    MODIFY COLUMN EXCISE_CODE INT UNSIGNED NULL;

-- 2. PASSWORD_RESET_REQUEST: the client identifies a reset request by a random, unguessable
--    token (never by the sequential ID). Only its hash is stored; case-sensitive for exact match.
ALTER TABLE PASSWORD_RESET_REQUEST
    ADD COLUMN REQUEST_TOKEN_HASH VARCHAR(255) NOT NULL COLLATE utf8mb4_0900_as_cs AFTER USER_ID,
    ADD UNIQUE KEY UQ_PWRESET_TOKEN (REQUEST_TOKEN_HASH);

-- 3. Seed administrator credential so login can be exercised end to end.
--    user: admin   password: Admin@123   (PBKDF2-SHA256, 100000 iterations - Pbkdf2PasswordHasher format)
UPDATE USERS
SET PASSWORD_HASH         = 'PBKDF2.SHA256.100000.sITHVTTZCqWgXp7n3jThJg==.oInLFS7drPbj2vs17CP9BWW480w57m7WGJb6013Bmb0=',
    FORCE_PASSWORD_CHANGE = 0,
    IS_ACTIVE             = 1,
    IS_BLOCKED            = 0,
    FAILED_LOGIN_ATTEMPTS = 0,
    LAST_FAILED_LOGIN_AT  = NULL,
    LOCKED_UNTIL          = NULL,
    PASSWORD_CHANGED_AT   = NOW(),
    PASSWORD_EXPIRES_AT   = NULL
WHERE USERNAME = 'admin';
