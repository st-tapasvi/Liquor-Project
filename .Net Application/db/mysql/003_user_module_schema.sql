-- ============================================================================
-- ST.LiquorTNT - User Module schema (MySQL 8.0.46), database: st_tnt_liquor
-- Phase: DATABASE DESIGN (no application code yet).
-- Naming: CAPITAL_LETTERS (per User Module requirement). Built on the existing
--         CAPITAL tables (USERS, ROLES, COMPANY, EXCISE, USER_LOG, ALLOTEDPLANTS).
-- All changes are ADDITIVE: ADD COLUMN / CREATE TABLE only. Nothing is dropped.
-- Role rights / permissions are OUT OF SCOPE here (handled later).
-- Config values are seeded in the DB, never hardcoded in application code.
-- ============================================================================

SET NAMES utf8mb4;

-- ----------------------------------------------------------------------------
-- 1. USERS  (modify existing table - add the columns the module needs)
--    Already present: ROLE_ID, COMPANY_ID, EXCISE_CODE, ALLOTED_PLANT_ID,
--    FAILED_LOGIN_ATTEMPTS, IS_BLOCKED, LOCKED_UNTIL, LAST_LOGIN_AT,
--    PASSWORD_CHANGED_AT, IS_ACTIVE, audit fields.
-- ----------------------------------------------------------------------------
ALTER TABLE USERS
    ADD COLUMN LAST_FAILED_LOGIN_AT  DATETIME     NULL AFTER FAILED_LOGIN_ATTEMPTS,
        -- needed for the "3 wrong attempts in a SINGLE DAY" reset boundary
    ADD COLUMN LAST_LOGIN_IP         VARCHAR(45)  NULL AFTER LAST_LOGIN_AT,
        -- IPv4/IPv6 of the last successful login (audit / security)
    ADD COLUMN PASSWORD_EXPIRES_AT   DATETIME     NULL AFTER PASSWORD_CHANGED_AT,
        -- computed from policy when expiry is enabled; NULL = never expires
    ADD COLUMN FORCE_PASSWORD_CHANGE TINYINT(1)   NOT NULL DEFAULT 0 AFTER PASSWORD_EXPIRES_AT;
        -- admin-created temp password -> user must change on first login

-- ----------------------------------------------------------------------------
-- 2. SECURITY_CONFIG  (GLOBAL - single super-admin managed configuration)
--    Key/value store so the admin panel can change security behaviour without
--    a code change. DATA_TYPE tells the app how to parse VALUE.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SECURITY_CONFIG (
    ID            INT UNSIGNED  NOT NULL AUTO_INCREMENT,
    CONFIG_KEY    VARCHAR(60)   NOT NULL,
    CONFIG_VALUE  VARCHAR(200)  NOT NULL,
    DATA_TYPE     VARCHAR(10)   NOT NULL DEFAULT 'STRING',   -- INT | BOOL | STRING
    DESCRIPTION   VARCHAR(255)  NULL,
    IS_ACTIVE     TINYINT(1)    NOT NULL DEFAULT 1,
    UPDATED_BY    INT           NULL,
    UPDATED_AT    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_SECURITY_CONFIG_KEY (CONFIG_KEY)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- INSERT IGNORE: re-running this script must never overwrite values an administrator has changed.
INSERT IGNORE INTO SECURITY_CONFIG (CONFIG_KEY, CONFIG_VALUE, DATA_TYPE, DESCRIPTION) VALUES
    ('FAILED_LOGIN_LOCK_ENABLED',     '1',    'BOOL', 'Master switch for failed-login account locking'),
    ('MAX_FAILED_LOGIN_ATTEMPTS',     '3',    'INT',  'Consecutive wrong passwords (per day) before lock'),
    ('ACCOUNT_LOCK_DURATION_MINUTES', '1440', 'INT',  'How long an account stays locked (1440 = 24h)'),
    ('SESSION_LIMIT_ENABLED',         '1',    'BOOL', 'Master switch for active-session limit'),
    ('MAX_ACTIVE_SESSIONS',           '2',    'INT',  'Max concurrent active sessions per user'),
    ('SESSION_EXPIRY_MINUTES',        '1440', 'INT',  'Idle/absolute session expiry in minutes'),
    ('SESSION_FULL_BEHAVIOUR',        'REJECT','STRING','What to do when max sessions reached (REJECT)'),
    ('SECURITY_QUESTION_ENABLED',     '1',    'BOOL', 'Master switch for forgot-password security questions'),
    ('SECURITY_QUESTION_REQUIRED',    '1',    'INT',  'How many security questions a user must set'),
    ('PASSWORD_RESET_EXPIRY_MINUTES', '15',   'INT',  'Validity of a forgot-password reset request'),
    ('PASSWORD_RESET_MAX_ATTEMPTS',   '5',    'INT',  'Max security-answer attempts per reset request'),
    ('ADMIN_ROLE_ID',                 '1',    'INT',  'Role allowed on user-management / security-admin endpoints until role rights exist');

-- ----------------------------------------------------------------------------
-- 3. PASSWORD_POLICY  (rules live in DB, names EASY/MEDIUM/HARD carry NO code behaviour)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS PASSWORD_POLICY (
    ID                        INT UNSIGNED NOT NULL AUTO_INCREMENT,
    POLICY_NAME               VARCHAR(50)  NOT NULL,
    MIN_LENGTH                INT          NOT NULL DEFAULT 8,
    MAX_LENGTH                INT          NOT NULL DEFAULT 64,
    REQUIRE_UPPERCASE         TINYINT(1)   NOT NULL DEFAULT 0,
    REQUIRE_LOWERCASE         TINYINT(1)   NOT NULL DEFAULT 0,
    REQUIRE_NUMBER            TINYINT(1)   NOT NULL DEFAULT 0,
    REQUIRE_SPECIAL_CHARACTER TINYINT(1)   NOT NULL DEFAULT 0,
    PASSWORD_HISTORY_COUNT    INT          NOT NULL DEFAULT 5,
    PASSWORD_EXPIRY_ENABLED   TINYINT(1)   NOT NULL DEFAULT 0,
    PASSWORD_EXPIRY_DAYS      INT          NULL,
    ALLOW_USERNAME_IN_PASSWORD TINYINT(1)  NOT NULL DEFAULT 0,
    ALLOW_COMMON_PASSWORD     TINYINT(1)   NOT NULL DEFAULT 0,
    STATUS                    TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_BY                INT          NULL,
    CREATED_AT                DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UPDATED_BY                INT          NULL,
    UPDATED_AT                DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_PASSWORD_POLICY_NAME (POLICY_NAME)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO PASSWORD_POLICY
    (POLICY_NAME, MIN_LENGTH, MAX_LENGTH, REQUIRE_UPPERCASE, REQUIRE_LOWERCASE,
     REQUIRE_NUMBER, REQUIRE_SPECIAL_CHARACTER, PASSWORD_HISTORY_COUNT,
     PASSWORD_EXPIRY_ENABLED, PASSWORD_EXPIRY_DAYS)
VALUES
    ('EASY',   6,  20, 0, 0, 1, 0, 3, 0, NULL),
    ('MEDIUM', 8,  30, 1, 1, 1, 0, 5, 0, NULL),
    ('HARD',   12, 64, 1, 1, 1, 1, 5, 1, 90);

-- ----------------------------------------------------------------------------
-- 4. ROLE_PASSWORD_POLICY  (which policy applies to which role - one per role)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ROLE_PASSWORD_POLICY (
    ID                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ROLE_ID            INT UNSIGNED NOT NULL,
    PASSWORD_POLICY_ID INT UNSIGNED NOT NULL,
    CREATED_BY         INT          NULL,
    CREATED_AT         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UPDATED_BY         INT          NULL,
    UPDATED_AT         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_ROLE_PASSWORD_POLICY_ROLE (ROLE_ID),
    CONSTRAINT FK_ROLE_PWPOLICY_ROLE   FOREIGN KEY (ROLE_ID)            REFERENCES ROLES (ID),
    CONSTRAINT FK_ROLE_PWPOLICY_POLICY FOREIGN KEY (PASSWORD_POLICY_ID) REFERENCES PASSWORD_POLICY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Administrator (role 1) -> HARD policy
INSERT IGNORE INTO ROLE_PASSWORD_POLICY (ROLE_ID, PASSWORD_POLICY_ID)
SELECT 1, p.ID FROM PASSWORD_POLICY p WHERE p.POLICY_NAME = 'HARD';

-- ----------------------------------------------------------------------------
-- 5. USER_PASSWORD_HISTORY  (prevent reuse of last N passwords)
--    No audit-by fields: a history row is a system-written fact, not user-edited.
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS USER_PASSWORD_HISTORY (
    ID            INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID       INT UNSIGNED NOT NULL,
    PASSWORD_HASH VARCHAR(255) NOT NULL COLLATE utf8mb4_0900_as_cs,
    CREATED_AT    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    KEY IDX_PWHIST_USER_CREATED (USER_ID, CREATED_AT),   -- fetch latest N for a user
    CONSTRAINT FK_PWHIST_USER FOREIGN KEY (USER_ID) REFERENCES USERS (ID) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 6. USER_SESSION  (server-side sessions: max-active limit, expiry, revoke)
--    Raw token is NEVER stored - only its hash (case-sensitive for exact match).
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS USER_SESSION (
    ID                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID            INT UNSIGNED NOT NULL,
    SESSION_TOKEN_HASH VARCHAR(255) NOT NULL COLLATE utf8mb4_0900_as_cs,
    LOGIN_AT           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    LAST_ACTIVITY_AT   DATETIME     NULL,
    EXPIRES_AT         DATETIME     NOT NULL,
    LOGOUT_AT          DATETIME     NULL,
    IP_ADDRESS         VARCHAR(45)  NULL,
    USER_AGENT         VARCHAR(255) NULL,
    DEVICE_INFO        VARCHAR(255) NULL,
    STATUS             VARCHAR(15)  NOT NULL DEFAULT 'ACTIVE',  -- ACTIVE | EXPIRED | REVOKED | LOGGED_OUT
    CREATED_AT         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_SESSION_TOKEN (SESSION_TOKEN_HASH),        -- validate token per request
    KEY IDX_SESSION_USER_STATUS (USER_ID, STATUS),          -- count active sessions on login
    KEY IDX_SESSION_EXPIRES (EXPIRES_AT),                   -- background expiry sweep
    CONSTRAINT FK_SESSION_USER FOREIGN KEY (USER_ID) REFERENCES USERS (ID) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 7. SECURITY_QUESTION  (master list of questions)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS SECURITY_QUESTION (
    ID            INT UNSIGNED NOT NULL AUTO_INCREMENT,
    QUESTION_TEXT VARCHAR(200) NOT NULL,
    STATUS        TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_AT    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UPDATED_AT    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_SECURITY_QUESTION_TEXT (QUESTION_TEXT)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT IGNORE INTO SECURITY_QUESTION (QUESTION_TEXT) VALUES
    ('What is the name of your first school?'),
    ('What is your mother''s maiden name?'),
    ('What was the name of your first pet?'),
    ('In which city were you born?'),
    ('What is your favourite book?');

-- ----------------------------------------------------------------------------
-- 8. USER_SECURITY_QUESTION  (a user's chosen questions + HASHED answers)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS USER_SECURITY_QUESTION (
    ID          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID     INT UNSIGNED NOT NULL,
    QUESTION_ID INT UNSIGNED NOT NULL,
    ANSWER_HASH VARCHAR(255) NOT NULL COLLATE utf8mb4_0900_as_cs,   -- never plaintext
    IS_ACTIVE   TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_AT  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UPDATED_AT  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_USER_QUESTION (USER_ID, QUESTION_ID),
    CONSTRAINT FK_USQ_USER     FOREIGN KEY (USER_ID)     REFERENCES USERS (ID) ON DELETE CASCADE,
    CONSTRAINT FK_USQ_QUESTION FOREIGN KEY (QUESTION_ID) REFERENCES SECURITY_QUESTION (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 9. PASSWORD_RESET_REQUEST  (forgot-password state: expiry + attempt limiting)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS PASSWORD_RESET_REQUEST (
    ID             INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID        INT UNSIGNED NOT NULL,
    STATUS         VARCHAR(15)  NOT NULL DEFAULT 'PENDING',   -- PENDING | VERIFIED | USED | EXPIRED | FAILED
    VERIFY_ATTEMPTS INT         NOT NULL DEFAULT 0,
    EXPIRES_AT     DATETIME     NOT NULL,
    CREATED_AT     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UPDATED_AT     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    KEY IDX_PWRESET_USER (USER_ID, STATUS),
    CONSTRAINT FK_PWRESET_USER FOREIGN KEY (USER_ID) REFERENCES USERS (ID) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ----------------------------------------------------------------------------
-- 10. USER_LOG  (modify existing audit table - add richer audit columns)
--     Already present: USER_ID, PAGE_ID, ACTION_TYPE, DATE_TIME, EXCISE_CODE,
--     ALLOTED_PLANT_ID, IP_ADDRESS, OLD_VALUE(json), NEW_VALUE(json), DEVICE_INFO.
-- ----------------------------------------------------------------------------
ALTER TABLE USER_LOG
    ADD COLUMN MODULE_NAME    VARCHAR(100) NULL AFTER ACTION_TYPE,
    ADD COLUMN ENTITY_NAME    VARCHAR(100) NULL AFTER MODULE_NAME,
    ADD COLUMN ENTITY_ID      VARCHAR(50)  NULL AFTER ENTITY_NAME,
    ADD COLUMN ACTION_STATUS  VARCHAR(20)  NULL AFTER ENTITY_ID,   -- SUCCESS | FAILED
    ADD COLUMN DESCRIPTION    VARCHAR(500) NULL AFTER ACTION_STATUS,
    ADD COLUMN USER_AGENT     VARCHAR(255) NULL AFTER IP_ADDRESS,
    ADD COLUMN CORRELATION_ID VARCHAR(64)  NULL AFTER USER_AGENT;

ALTER TABLE USER_LOG
    ADD KEY IDX_USERLOG_USER_TIME (USER_ID, DATE_TIME),   -- audit screen: per-user timeline
    ADD KEY IDX_USERLOG_ACTION (ACTION_TYPE);             -- filter by action type
