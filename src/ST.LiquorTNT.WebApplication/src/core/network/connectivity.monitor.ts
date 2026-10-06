import { env } from '../config';
import { logger } from '../logging/logger';

import { useConnectivityStore } from './connectivity.store';

export interface ConnectivityMonitorOptions {
  probe?: () => Promise<boolean>;
  retryDelayMs?: (attempt: number) => number;
}

export const defaultRetryDelayMs = (attempt: number): number => Math.min(2000 * 2 ** attempt, 15_000);

const PROBE_TIMEOUT_MS = 5000;

export function healthUrl(apiBaseUrl: string = env.VITE_API_BASE_URL, origin: string = window.location.origin): string {
  const base = new URL(apiBaseUrl.endsWith('/') ? apiBaseUrl : `${apiBaseUrl}/`, origin);
  return new URL('../health', base).toString();
}

export async function probeHealth(): Promise<boolean> {
  try {
    const response = await fetch(healthUrl(), {
      method: 'GET',
      cache: 'no-store',
      credentials: 'omit',
      signal: AbortSignal.timeout(PROBE_TIMEOUT_MS),
    });
    return response.ok;
  } catch {
    return false;
  }
}

interface ActiveMonitor {
  check: () => void;
}
let active: ActiveMonitor | null = null;

export function requestConnectivityCheck(): void {
  active?.check();
}

export function startConnectivityMonitor(options: ConnectivityMonitorOptions = {}): () => void {
  const probe = options.probe ?? probeHealth;
  const retryDelayMs = options.retryDelayMs ?? defaultRetryDelayMs;

  let timer: ReturnType<typeof setTimeout> | undefined;
  let attempt = 0;
  let probing = false;
  let stopped = false;

  const cancelTimer = () => {
    if (timer !== undefined) clearTimeout(timer);
    timer = undefined;
  };

  const schedule = () => {
    cancelTimer();
    if (stopped || useConnectivityStore.getState().status !== 'offline') return;
    timer = setTimeout(() => void runProbe(), retryDelayMs(attempt));
    attempt += 1;
  };

  const runProbe = async () => {
    if (stopped || probing) return;
    probing = true;
    let reachable: boolean;
    try {
      reachable = await probe();
    } catch {
      reachable = false;
    } finally {
      probing = false;
    }
    if (stopped) return;

    const store = useConnectivityStore.getState();
    if (reachable) {
      if (store.status === 'offline') logger.info('API reachable again');
      store.markOnline();
    } else {
      store.markOffline();
      schedule();
    }
  };

  const unsubscribe = useConnectivityStore.subscribe((state, previous) => {
    if (state.status === previous.status) return;
    if (state.status === 'offline') {
      logger.warn('API unreachable; retrying in the background');
      attempt = 0;
      schedule();
    } else {
      cancelTimer();
    }
  });

  const onBrowserOffline = () => useConnectivityStore.getState().markOffline();
  const onBrowserOnline = () => void runProbe();
  window.addEventListener('offline', onBrowserOffline);
  window.addEventListener('online', onBrowserOnline);

  active = { check: () => void runProbe() };
  if (useConnectivityStore.getState().status === 'offline') schedule();

  return () => {
    stopped = true;
    cancelTimer();
    unsubscribe();
    window.removeEventListener('offline', onBrowserOffline);
    window.removeEventListener('online', onBrowserOnline);
    active = null;
  };
}
