import { healthUrl, startConnectivityMonitor } from './connectivity.monitor';
import { useConnectivityStore } from './connectivity.store';

const status = () => useConnectivityStore.getState().status;

describe('connectivity monitor', () => {
  let stop: (() => void) | undefined;

  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    stop?.();
    stop = undefined;
    vi.useRealTimers();
  });

  it('does nothing while the API is reachable', async () => {
    const probe = vi.fn().mockResolvedValue(true);
    stop = startConnectivityMonitor({ probe, retryDelayMs: () => 1000 });
    await vi.advanceTimersByTimeAsync(10_000);
    expect(probe).not.toHaveBeenCalled();
  });

  it('probes with back-off while offline and goes online when the API answers', async () => {
    const probe = vi.fn().mockResolvedValueOnce(false).mockResolvedValueOnce(false).mockResolvedValue(true);
    stop = startConnectivityMonitor({ probe, retryDelayMs: (n) => 1000 * (n + 1) });

    useConnectivityStore.getState().markOffline();

    await vi.advanceTimersByTimeAsync(1000); // 1st probe: still down
    expect(probe).toHaveBeenCalledTimes(1);
    expect(status()).toBe('offline');

    await vi.advanceTimersByTimeAsync(1999); // 2nd probe is due after 2 s
    expect(probe).toHaveBeenCalledTimes(1);
    await vi.advanceTimersByTimeAsync(1);
    expect(probe).toHaveBeenCalledTimes(2);

    await vi.advanceTimersByTimeAsync(3000); // 3rd probe: back
    expect(probe).toHaveBeenCalledTimes(3);
    expect(status()).toBe('online');

    await vi.advanceTimersByTimeAsync(60_000); // and it stops probing
    expect(probe).toHaveBeenCalledTimes(3);
  });

  it('a request that succeeds in the meantime ends the retries', async () => {
    const probe = vi.fn().mockResolvedValue(false);
    stop = startConnectivityMonitor({ probe, retryDelayMs: () => 1000 });
    useConnectivityStore.getState().markOffline();
    await vi.advanceTimersByTimeAsync(1000);
    expect(probe).toHaveBeenCalledTimes(1);

    useConnectivityStore.getState().markOnline();
    await vi.advanceTimersByTimeAsync(10_000);
    expect(probe).toHaveBeenCalledTimes(1);
  });

  it('reacts to the browser losing and regaining the network', async () => {
    const probe = vi.fn().mockResolvedValue(true);
    stop = startConnectivityMonitor({ probe, retryDelayMs: () => 60_000 });

    window.dispatchEvent(new Event('offline'));
    expect(status()).toBe('offline');

    window.dispatchEvent(new Event('online'));
    await vi.advanceTimersByTimeAsync(0);
    expect(probe).toHaveBeenCalledTimes(1);
    expect(status()).toBe('online');
  });

  it('stops cleanly', async () => {
    const probe = vi.fn().mockResolvedValue(false);
    startConnectivityMonitor({ probe, retryDelayMs: () => 1000 })();
    useConnectivityStore.getState().markOffline();
    await vi.advanceTimersByTimeAsync(5000);
    expect(probe).not.toHaveBeenCalled();
  });
});

describe('healthUrl', () => {
  it('sits next to the /api prefix', () => {
    expect(healthUrl('/api', 'http://localhost:5173')).toBe('http://localhost:5173/health');
    expect(healthUrl('/liquor/api/', 'https://srv')).toBe('https://srv/liquor/health');
    expect(healthUrl('https://api.local/api', 'http://localhost:5173')).toBe('https://api.local/health');
  });
});
