-- =============================================================================
-- 008  Safety default for USER_SESSION.ABSOLUTE_EXPIRES_AT
--      007 added this column as NOT NULL with no default. A build made before the
--      sliding-session change does not set it, so its session INSERT fails and login breaks.
--      Giving it a default (login time + 1 day) lets such a build keep working:
--      the new build still sets the value explicitly, so this only helps a mismatched old one.
--      Run once, after 007.
-- =============================================================================

ALTER TABLE USER_SESSION
    MODIFY COLUMN ABSOLUTE_EXPIRES_AT DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL 1 DAY);
