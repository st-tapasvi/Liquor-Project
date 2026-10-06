import { create } from 'zustand';

export type ConnectivityStatus = 'online' | 'offline';

interface ConnectivityState {
  status: ConnectivityStatus;
  offlineSince: number | null;
  markOffline: () => void;
  markOnline: () => void;
}

function initialStatus(): ConnectivityStatus {
  return typeof navigator !== 'undefined' && !navigator.onLine ? 'offline' : 'online';
}

export const useConnectivityStore = create<ConnectivityState>()((set, get) => ({
  status: initialStatus(),
  offlineSince: initialStatus() === 'offline' ? Date.now() : null,

  markOffline: () => {
    if (get().status !== 'offline') set({ status: 'offline', offlineSince: Date.now() });
  },
  markOnline: () => {
    if (get().status !== 'online') set({ status: 'online', offlineSince: null });
  },
}));

export const connectivity = {
  isOnline: () => useConnectivityStore.getState().status === 'online',
  markOffline: () => useConnectivityStore.getState().markOffline(),
  markOnline: () => useConnectivityStore.getState().markOnline(),
};

export function useConnectivity(): ConnectivityStatus {
  return useConnectivityStore((s) => s.status);
}
