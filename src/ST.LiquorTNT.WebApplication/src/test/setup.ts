import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import toast from 'react-hot-toast';
import { afterEach } from 'vitest';

import { useReauthStore, useSessionStore } from '@/core/auth';
import { useModuleFlagsStore } from '@/core/modules';
import { useConnectivityStore } from '@/core/network';
import { useTenantStore } from '@/core/tenant';

afterEach(() => {
  cleanup();
  toast.remove();
  useConnectivityStore.setState({ status: 'online', offlineSince: null });
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
