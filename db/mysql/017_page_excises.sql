-- ============================================================================
-- ST.LiquorTNT - which excises (states) a page and a page action belong to.
-- Run ONCE, after 016_role_groups.sql.
--
-- Pages of both applications (WEB and LINE) can depend on the excise: some screens exist in every state (User, Role,
-- Settings), some only in a few. Owner decision (2026-10-12): mapping tables with foreign keys to EXCISE, so the
-- database itself refuses an unknown excise and keeps the links clean.
--
--   PAGE_EXCISES          no row for a page    → the page exists in every excise
--                         rows RJ, UP          → only in RJ and UP
--   PAGE_ACTION_EXCISES   no row for an action → the action exists wherever its page does
--                         row RJ               → only in RJ (must be one of its page's excises; checked by a test)
--
-- An action counts in a supplier code only when its page AND the action cover that supplier code's excise:
--   * a role's rights grid shows the pages of the role's excise(s);
--   * the rights a user gets in the active supplier code leave out pages / actions of other excises;
--   * giving a role or a custom right an action of another excise is refused.
-- Seed scripts never write excise ids by hand: they look them up by code (… WHERE EXCISE_CODE = 'RJ'), because the
-- ids differ between installations.
-- ============================================================================

CREATE TABLE IF NOT EXISTS PAGE_EXCISES (
    ID         INT UNSIGNED NOT NULL AUTO_INCREMENT,
    PAGE_ID    INT UNSIGNED NOT NULL,
    EXCISE_ID  INT UNSIGNED NOT NULL,
    CREATED_AT DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_PAGE_EXCISE (PAGE_ID, EXCISE_ID),
    KEY IDX_PAGE_EXCISES_EXCISE (EXCISE_ID),
    CONSTRAINT FK_PAGE_EXCISES_PAGE   FOREIGN KEY (PAGE_ID)   REFERENCES PAGES (ID)  ON DELETE CASCADE,
    CONSTRAINT FK_PAGE_EXCISES_EXCISE FOREIGN KEY (EXCISE_ID) REFERENCES EXCISE (EXCISE_ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS PAGE_ACTION_EXCISES (
    ID             INT UNSIGNED NOT NULL AUTO_INCREMENT,
    PAGE_ACTION_ID INT UNSIGNED NOT NULL,
    EXCISE_ID      INT UNSIGNED NOT NULL,
    CREATED_AT     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_PAGE_ACTION_EXCISE (PAGE_ACTION_ID, EXCISE_ID),
    KEY IDX_PAGE_ACTION_EXCISES_EXCISE (EXCISE_ID),
    CONSTRAINT FK_PAGE_ACTION_EXCISES_ACTION FOREIGN KEY (PAGE_ACTION_ID) REFERENCES PAGE_ACTIONS (ID) ON DELETE CASCADE,
    CONSTRAINT FK_PAGE_ACTION_EXCISES_EXCISE FOREIGN KEY (EXCISE_ID)      REFERENCES EXCISE (EXCISE_ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- No rows yet: every page and action that exists today is used in every excise (018 then limits them to RJ and JK).
