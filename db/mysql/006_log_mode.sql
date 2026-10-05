-- =============================================================================
-- 006  Application log mode (admin panel: PUT /api/securityconfig/LOG_MODE)
--      NORMAL : one line per request, warnings and errors
--      DETAIL : + request/response bodies, every Business method's input/output/exception, SQL
--      The API re-reads it every 10 seconds; no restart needed. Secrets are masked in both modes.
-- =============================================================================

INSERT IGNORE INTO SECURITY_CONFIG (CONFIG_KEY, CONFIG_VALUE, DATA_TYPE, DESCRIPTION) VALUES
    ('LOG_MODE', 'NORMAL', 'STRING', 'Application log detail: NORMAL or DETAIL (bodies, method input/output, SQL)');
