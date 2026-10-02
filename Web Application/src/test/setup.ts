import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterAll, afterEach, beforeAll } from 'vitest';

import { useReauthStore, useSessionStore } from '@/core/auth';
import { useModuleFlagsStore } from '@/core/modules';
import { useTenantStore } from '@/core/tenant';

import { mockAuthState, mockPlatformState } from './msw/handlers';
import { server } from './msw/server';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

afterEach(() => {
  cleanup();
  server.resetHandlers();
  mockAuthState.reset();
  mockPlatformState.reset();
  useSessionStore.setState({
    status: 'unknown',
    user: null,
    expiresAt: null,
    idleTimeoutMinutes: null,
    endReason: null,
  });
  useReauthStore.setState({ isOpen: false, waiters: [] });
  useModuleFlagsStore.setState({ loaded: false, enabled: new Set() });
  useTenantStore.setState({
    companyId: null,
    companyName: null,
    exciseCode: null,
    plantId: null,
    plantName: null,
    companies: [],
    plants: [],
    loaded: false,
  });
});

afterAll(() => server.close());

// jsdom lacks matchMedia, which MUI's useMediaQuery needs.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
  }),
});
