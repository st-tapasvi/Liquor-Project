import { create } from 'zustand';

interface Waiter {
  resolve: () => void;
  reject: (reason: unknown) => void;
}

interface ReauthState {
  isOpen: boolean;
  waiters: Waiter[];
  waitForReauth: () => Promise<void>;
  resolve: () => void;
  reject: (reason: unknown) => void;
}

export const useReauthStore = create<ReauthState>()((set, get) => ({
  isOpen: false,
  waiters: [],

  waitForReauth: () =>
    new Promise<void>((resolve, reject) => {
      set((s) => ({ isOpen: true, waiters: [...s.waiters, { resolve, reject }] }));
    }),

  resolve: () => {
    const { waiters } = get();
    set({ isOpen: false, waiters: [] });
    for (const w of waiters) w.resolve();
  },

  reject: (reason) => {
    const { waiters } = get();
    set({ isOpen: false, waiters: [] });
    for (const w of waiters) w.reject(reason);
  },
}));

export const reauthStore = {
  waitForReauth: () => useReauthStore.getState().waitForReauth(),
  resolve: () => useReauthStore.getState().resolve(),
  reject: (reason: unknown) => useReauthStore.getState().reject(reason),
};
