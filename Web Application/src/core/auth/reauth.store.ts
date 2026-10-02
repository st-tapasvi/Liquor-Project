import { create } from 'zustand';

/**
 * Coordinates the "session reached its hard limit" flow (401 SESSION_EXPIRED):
 *
 *   1. A request fails with SESSION_EXPIRED. The error interceptor parks it here (`waitForReauth`)
 *      and asks the UI to open the password dialog (`isOpen = true`). Further failures park too.
 *   2. The user re-enters the password. The dialog calls login and then `resolve()`.
 *      Every parked request is retried with the new session cookie, in order.
 *   3. If the user cancels, `reject()` fails the parked requests and the session is ended.
 *
 * Nothing on screen is lost; TanStack Query simply sees the retried promise settle.
 */
interface Waiter {
  resolve: () => void;
  reject: (reason: unknown) => void;
}

interface ReauthState {
  isOpen: boolean;
  waiters: Waiter[];
  /** Returns a promise that settles when the user has re-authenticated (or cancelled). */
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
