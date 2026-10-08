export type LogLevel = 'debug' | 'info' | 'warn' | 'error';

export const LOG_LEVEL_ORDER: Record<LogLevel, number> = { debug: 10, info: 20, warn: 30, error: 40 };

export type LogContext = Record<string, unknown>;

export interface LogEntry {
  level: LogLevel;
  message: string;
  timestamp: string;
  context: LogContext;
  meta: {
    appVersion: string;
    userId: number | null;
    route: string;
    correlationId: string | null;
  };
}

export interface LogTransport {
  write: (entry: LogEntry) => void;
  flush?: () => void;
}
