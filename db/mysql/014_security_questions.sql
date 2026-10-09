-- ============================================================================
-- ST.LiquorTNT - 10 security questions, and a question in use can never change.
-- Run ONCE, after 013_page_application_type.sql.
--
-- A user's answer belongs to the exact question text the user saw. If that text changed later, the stored answer
-- would belong to a different question and "forgot password" would ask something the user never answered. So:
--   * DELETE of a question any user has chosen (now or before) is refused by FK_USQ_QUESTION (already there);
--   * UPDATE of QUESTION_TEXT of such a question is refused by the trigger below;
--   * STATUS may still go to 0 to retire a question: it leaves the list for new choices, while users who chose it
--     keep it and forgot password keeps asking it;
--   * the text of a question nobody has chosen yet may still be corrected.
-- The application has no API that edits questions; questions are added with SQL scripts like this one.
-- ============================================================================

INSERT IGNORE INTO SECURITY_QUESTION (QUESTION_TEXT, STATUS) VALUES
    ('What was the name of your childhood best friend?', 1),
    ('What was the model of your first vehicle?', 1),
    ('What is the name of the street you grew up on?', 1),
    ('What was the name of the company where you had your first job?', 1),
    ('What is the name of your favourite teacher?', 1);

DROP TRIGGER IF EXISTS TRG_SECURITY_QUESTION_LOCK_TEXT;

DELIMITER //
CREATE TRIGGER TRG_SECURITY_QUESTION_LOCK_TEXT
BEFORE UPDATE ON SECURITY_QUESTION
FOR EACH ROW
BEGIN
    -- byte compare, so even a change of upper / lower case counts as a change
    IF NOT (CAST(NEW.QUESTION_TEXT AS BINARY) <=> CAST(OLD.QUESTION_TEXT AS BINARY))
       AND EXISTS (SELECT 1 FROM USER_SECURITY_QUESTION u WHERE u.QUESTION_ID = OLD.ID) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Security question is in use: its text cannot change. Retire it (STATUS=0) and add a new one.';
    END IF;
END//
DELIMITER ;
