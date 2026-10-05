import type { LogEntry, LogTransport } from '../types';

/**
 * Production transport: batches entries and posts them to the API's client-log endpoint.
 *
 * Uses `fetch` with `keepalive` (not the axios instance) on purpose: it must work while the page is
 * unloading and it must never trigger the error interceptor (a failing log call must not log itself).
 * The endpoint is same-origin, so the session cookie is attached and the API can tag entries with the
 * user. Entries are already redacted by the logger before they get here.
 *
 * Until `POST /api/client-logs` exists in the API this transport is created with `enabled: false`
 * and simply drops entries; enabling it is a one-line change in logger.ts.
 */
export interface RemoteTransportOptions {
  url: string;
  enabled: boolean;
  flushIntervalMs?: number;
  maxBatch?: number;
}

export function createRemoteTransport(options: RemoteTransportOptions): LogTransport {
  const { url, enabled, flushIntervalMs = 10_000, maxBatch = 20 } = options;
  let buffer: LogEntry[] = [];
  let timer: ReturnType<typeof setTimeout> | undefined;

  const send = (entries: LogEntry[]) => {
    if (!enabled || entries.length === 0) return;
    try {
      void fetch(url, {
        method: 'POST',
        credentials: 'same-origin',
        keepalive: true,
        headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
        body: JSON.stringify({ entries }),
      }).catch(() => undefined);
    } catch {
      // Logging must never throw.
    }
  };

  const flush = () => {
    if (timer) {
      clearTimeout(timer);
      timer = undefined;
    }
    const batch = buffer;
    buffer = [];
    send(batch);
  };

  if (enabled && typeof document !== 'undefined') {
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') flush();
    });
  }

  return {
    write(entry) {
      if (!enabled) return;
      buffer.push(entry);
      if (entry.level === 'error' || buffer.length >= maxBatch) {
        flush();
        return;
      }
      timer ??= setTimeout(flush, flushIntervalMs);
    },
    flush,
  };
}
