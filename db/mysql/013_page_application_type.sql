-- ============================================================================
-- ST.LiquorTNT - which application a page belongs to.
-- Run ONCE, after 012_supplier_code_roles.sql.
--
-- Two applications share roles and rights: the WEB application (server) and the LINE application (desktop at the
-- production line). Every page belongs to exactly one of them; a job done in both gets two pages with their own
-- keys (e.g. 'batch' WEB and 'linebatch' LINE). A role may hold rights of both applications.
--
-- PAGE_NAME is now unique per application (both may have a "Batch" page); PAGE_KEY stays unique overall,
-- because it is the first part of every permission key.
-- ============================================================================

ALTER TABLE PAGES
    ADD COLUMN APPLICATION_TYPE VARCHAR(10) NOT NULL DEFAULT 'WEB' AFTER PAGE_KEY,
    ADD CONSTRAINT CHK_PAGES_APPLICATION_TYPE CHECK (APPLICATION_TYPE IN ('WEB', 'LINE'));

ALTER TABLE PAGES
    ADD UNIQUE KEY UQ_PAGES_APPLICATION_NAME (APPLICATION_TYPE, PAGE_NAME),
    DROP INDEX UNIQ_PAGE_NAME;

-- every page that exists today is a page of the web application (the default above already set it)
UPDATE PAGES SET APPLICATION_TYPE = 'WEB';
