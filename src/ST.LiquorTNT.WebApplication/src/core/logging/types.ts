export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

export const LOG_LEVEL_ORDER: Record<LogLevel, number> = { debug: 10, info: 20, warn: 30, error: 40 };

/** Structured context attached to an entry. Only ids and codes: never PII, credentials or bodies. */
export type LogContext = Record<string, unknown>;

export interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  context: LogContext;
  /** Filled by the logger from the current session / route. */
  meta: {
    appVersion: string;
    userId: number | null;
    route: string;
    correlationId: string | null;
  };
}

export interface LogTransport {
  write: (entry: LogEntry) => void;
  /** Flush buffered entries (remote transport); called on page hide and on fatal errors. */
  flush?: () => void;
}
