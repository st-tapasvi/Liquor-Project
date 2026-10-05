import type { LogEntry, LogTransport } from '../types';

/** Development transport. The only file in the codebase allowed to call `console`. */
export const consoleTransport: LogTransport = {
  write(entry: LogEntry) {
    const line = `[${entry.timestamp}] ${entry.level.toUpperCase()} ${entry.message}`;
    const payload = { ...entry.context, ...entry.meta };
    switch (entry.level) {
      case 'debug':
        console.debug(line, payload);
        break;
      case 'info':
        console.info(line, payload);
        break;
      case 'warn':
        console.warn(line, payload);
        break;
      case 'error':
        console.error(line, payload);
        break;
    }
  },
};
