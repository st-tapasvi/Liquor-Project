-- ============================================================================
-- ST.LiquorTNT - role groups.
-- Run ONCE, after 015_admin_role_name.sql.
--
-- A role group is a named bundle of master roles of one company (e.g. "All Operators" = Operator RJ CL 772 +
-- Operator RJ IMFL 1028 + Operator JK IMFL 369). A user can get roles directly (USER_ROLES), through groups
-- (USER_ROLE_GROUPS), or both; the user's roles are the union. Changing a group changes it for all its users at once:
-- a new supplier code's role is ticked in the group once instead of being given to 50 users one by one.
--
--   * groups hold roles only (no rights, no other groups);
--   * a new supplier code's roles are NOT added to groups automatically - the admin ticks them (owner decision);
--   * a group given to any user cannot be deleted (409 ROLE_GROUP_IN_USE);
--   * a group may hold admin / company-level roles; such a group is managed only with user.manageadmin.
-- Group management uses the role rights (role.view / add / edit / delete); giving groups to users uses user.access.
-- ============================================================================

CREATE TABLE IF NOT EXISTS ROLE_GROUP (
    ID          INT UNSIGNED NOT NULL AUTO_INCREMENT,
    COMPANY_ID  INT UNSIGNED NOT NULL,
    GROUP_NAME  VARCHAR(100) NOT NULL,
    DESCRIPTION VARCHAR(255) NULL,
    IS_ACTIVE   TINYINT(1)   NOT NULL DEFAULT 1,
    CREATED_BY  INT          NULL,
    CREATED_AT  DATETIME     NULL,
    UPDATED_BY  INT          NULL,
    UPDATED_AT  DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_ROLE_GROUP_COMPANY_NAME (COMPANY_ID, GROUP_NAME),
    CONSTRAINT FK_ROLE_GROUP_COMPANY FOREIGN KEY (COMPANY_ID) REFERENCES COMPANY (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- the roles inside a group; a role still inside a group cannot be deleted
CREATE TABLE IF NOT EXISTS ROLE_GROUP_ROLES (
    ID            INT UNSIGNED NOT NULL AUTO_INCREMENT,
    ROLE_GROUP_ID INT UNSIGNED NOT NULL,
    ROLE_ID       INT UNSIGNED NOT NULL,
    CREATED_BY    INT          NULL,
    CREATED_AT    DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_ROLE_GROUP_ROLE (ROLE_GROUP_ID, ROLE_ID),
    KEY IDX_ROLE_GROUP_ROLES_ROLE (ROLE_ID),
    CONSTRAINT FK_ROLE_GROUP_ROLES_GROUP FOREIGN KEY (ROLE_GROUP_ID) REFERENCES ROLE_GROUP (ID) ON DELETE CASCADE,
    CONSTRAINT FK_ROLE_GROUP_ROLES_ROLE  FOREIGN KEY (ROLE_ID)       REFERENCES ROLES (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- the groups a user holds
CREATE TABLE IF NOT EXISTS USER_ROLE_GROUPS (
    ID            INT UNSIGNED NOT NULL AUTO_INCREMENT,
    USER_ID       INT UNSIGNED NOT NULL,
    ROLE_GROUP_ID INT UNSIGNED NOT NULL,
    CREATED_BY    INT          NULL,
    CREATED_AT    DATETIME     NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UQ_USER_ROLE_GROUP (USER_ID, ROLE_GROUP_ID),
    KEY IDX_USER_ROLE_GROUPS_GROUP (ROLE_GROUP_ID),
    CONSTRAINT FK_USER_ROLE_GROUPS_USER  FOREIGN KEY (USER_ID)       REFERENCES USERS (ID) ON DELETE CASCADE,
    CONSTRAINT FK_USER_ROLE_GROUPS_GROUP FOREIGN KEY (ROLE_GROUP_ID) REFERENCES ROLE_GROUP (ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
