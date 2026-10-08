import type { LogEntry, LogTransport } from '../types';

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
