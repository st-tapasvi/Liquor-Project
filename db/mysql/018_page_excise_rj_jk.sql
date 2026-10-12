-- ============================================================================
-- ST.LiquorTNT - today's pages and actions are used in Rajasthan (RJ) and Jharkhand (JK).
-- Run ONCE, after 017_page_excises.sql. Safe to re-run (INSERT IGNORE on the unique keys).
--
-- Owner, 2026-10-12: put RJ and JK on every page and page action. A supplier code of any other excise then gets
-- no rights and no menu until its excise is added, e.g. for Uttar Pradesh:
--   INSERT IGNORE INTO PAGE_EXCISES (PAGE_ID, EXCISE_ID)
--   SELECT p.PAGE_ID, e.EXCISE_ID FROM (SELECT DISTINCT PAGE_ID FROM PAGE_EXCISES) p JOIN EXCISE e ON e.EXCISE_CODE = 'UP';
--   (and the same for PAGE_ACTION_EXCISES / PAGE_ACTION_ID)
-- Excise ids are looked up by code, never written by hand.
-- ============================================================================

INSERT IGNORE INTO PAGE_EXCISES (PAGE_ID, EXCISE_ID)
SELECT p.ID, e.EXCISE_ID
FROM PAGES p
JOIN EXCISE e ON e.EXCISE_CODE IN ('RJ', 'JK');

INSERT IGNORE INTO PAGE_ACTION_EXCISES (PAGE_ACTION_ID, EXCISE_ID)
SELECT a.ID, e.EXCISE_ID
FROM PAGE_ACTIONS a
JOIN EXCISE e ON e.EXCISE_CODE IN ('RJ', 'JK');
