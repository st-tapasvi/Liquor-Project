-- ============================================================================
-- ST.LiquorTNT - User Module: remove direct tenant columns from USERS.
-- EXCISE_CODE and ALLOTED_PLANT_ID are dropped from USERS because a user's
-- plant/excise access is assigned separately (access/roles module, later),
-- not as single-value columns on the user row. COMPANY_ID is kept.
-- Drop the foreign keys first, then the columns (their indexes drop with them).
-- ============================================================================

ALTER TABLE USERS DROP FOREIGN KEY FK_USERS_EXCISE;
ALTER TABLE USERS DROP FOREIGN KEY FK_USERS_ALLOTED_PLANT_ID;

ALTER TABLE USERS DROP COLUMN EXCISE_CODE;
ALTER TABLE USERS DROP COLUMN ALLOTED_PLANT_ID;
