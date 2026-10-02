import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { App } from './app/App';

async function bootstrap() {
  // Mock API for local development without the backend. Never bundled in production (env.ts refuses it).
  // Checked on import.meta.env directly (not the parsed `env` object) so the bundler removes this
  // branch, and the mock worker, from production builds entirely.
  if (import.meta.env.DEV && import.meta.env.VITE_USE_MOCKS === 'true') {
    const { startMockWorker } = await import('./test/msw/browser');
    await startMockWorker();
  }

  const container = document.getElementById('root');
  if (!container) throw new Error('Root element #root not found.');

  createRoot(container).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}

void bootstrap();
