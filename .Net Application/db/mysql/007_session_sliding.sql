-- =============================================================================
-- 007  Sliding web sessions
--      USER_SESSION.EXPIRES_AT          = sliding deadline: last activity + SESSION_IDLE_MINUTES
--      USER_SESSION.ABSOLUTE_EXPIRES_AT = hard limit set at login: LOGIN_AT + SESSION_EXPIRY_MINUTES
--      Idle too long  -> 401 SESSION_TIMED_OUT (client goes to the login page)
--      Hard limit     -> 401 SESSION_EXPIRED   (client shows a password popup and retries the call)
--      Run once, after 003-006.
-- =============================================================================

ALTER TABLE USER_SESSION
    ADD COLUMN ABSOLUTE_EXPIRES_AT DATETIME NULL AFTER EXPIRES_AT;

-- Sessions that already exist keep the deadline they were created with.
UPDATE USER_SESSION SET ABSOLUTE_EXPIRES_AT = EXPIRES_AT WHERE ABSOLUTE_EXPIRES_AT IS NULL;

ALTER TABLE USER_SESSION
    MODIFY COLUMN ABSOLUTE_EXPIRES_AT DATETIME NOT NULL;

INSERT IGNORE INTO SECURITY_CONFIG (CONFIG_KEY, CONFIG_VALUE, DATA_TYPE, DESCRIPTION) VALUES
    ('SESSION_IDLE_MINUTES', '60', 'INT', 'Minutes without any API call before a web session ends (sliding window)');

-- The existing key keeps its value; only its meaning is spelt out.
UPDATE SECURITY_CONFIG
   SET DESCRIPTION = 'Hard limit of a web session in minutes, even while the user is working (1440 = 24h)'
 WHERE CONFIG_KEY = 'SESSION_EXPIRY_MINUTES';
