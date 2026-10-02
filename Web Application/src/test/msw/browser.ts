import { setupWorker } from 'msw/browser';

import { handlers } from './handlers';

/**
 * Browser-side mock API for `VITE_USE_MOCKS=true` (development only).
 * Requires the service worker file: `npx msw init public --save` (writes public/mockServiceWorker.js).
 */
export async function startMockWorker(): Promise<void> {
  const worker = setupWorker(...handlers);
  await worker.start({ onUnhandledRequest: 'bypass' });
}
