import { sessionStore } from '../auth/session.store';
import { appConfig, env } from '../config';

import { redact } from './redact';
import { consoleTransport } from './transports/console.transport';
import { createRemoteTransport } from './transports/remote.transport';
import { LOG_LEVEL_ORDER, type LogContext, type LogEntry, type LogLevel, type LogTransport } from './types';

let lastCorrelationId: string | null = null;

export function setLastCorrelationId(id: string | null): void {
  lastCorrelationId = id;
}

const minLevel: LogLevel = env.DEV ? 'debug' : env.VITE_LOG_LEVEL;

const transports: LogTransport[] = env.PROD
  ? [createRemoteTransport({ url: `${env.VITE_API_BASE_URL}/client-logs`, enabled: false })]
  : [consoleTransport];

function write(level: LogLevel, message: string, context: LogContext = {}): void {
  if (LOG_LEVEL_ORDER[level] < LOG_LEVEL_ORDER[minLevel]) return;

  const entry: LogEntry = {
    level,
    message,
    timestamp: new Date().toISOString(),
    context: redact(context) as LogContext,
    meta: {
      appVersion: appConfig.version,
      userId: sessionStore.get().user?.userId ?? null,
      route: typeof window !== 'undefined' ? window.location.pathname : '',
      correlationId: lastCorrelationId,
    },
  };

  for (const t of transports) {
    try {
      t.write(entry);
    } catch {
      // A transport failure must never break the application.
    }
  }
}

export const logger = {
  debug: (message: string, context?: LogContext) => write('debug', message, context),
  info: (message: string, context?: LogContext) => write('info', message, context),
  warn: (message: string, context?: LogContext) => write('warn', message, context),
  error: (message: string, context?: LogContext) => write('error', message, context),
  flush: () => transports.forEach((t) => t.flush?.()),
};

export type Logger = typeof logger;
