/**
 * The access token (bearer JWT) the API returns from POST /api/auth/login.
 *
 * The API issues the token in the response body and expects it back as `Authorization: Bearer …`
 * on every call (the HttpOnly-cookie design of ADR 0002 is not implemented on the server). So this is
 * the one place in the application that holds a credential:
 *
 *  - In memory while the tab lives; `authInterceptor` reads it on every request.
 *  - Mirrored to `sessionStorage` so a page reload (F5, deep link) keeps the session instead of
 *    forcing a new login — which would open a second USER_SESSION on the server and, after two
 *    reloads, hit MAX_ACTIVE_SESSIONS (409). `sessionStorage` is per tab and is wiped when the tab
 *    closes; nothing is ever written to `localStorage`.
 *
 * This file is the single exception to the storage ban in eslint.config.js; set `PERSIST` to false to
 * go memory-only (every reload then needs a login).
 */
const PERSIST = true;
const KEY = 'st.tnt.access-token';

let token: string | null = null;
let restored = false;

function storage(): Storage | null {
  if (!PERSIST || typeof window === 'undefined') return null;
  try {
    // eslint-disable-next-line no-restricted-properties -- the one sanctioned use, see the file comment
    return window.sessionStorage;
  } catch {
    return null; // storage blocked (privacy mode, iframe): run memory-only
  }
}

export const tokenStore = {
  /** The current token, restoring it from the tab's storage on first use after a reload. */
  get(): string | null {
    if (!restored) {
      restored = true;
      if (token === null) {
        try {
          token = storage()?.getItem(KEY) ?? null;
        } catch {
          token = null;
        }
      }
    }
    return token;
  },

  set(value: string): void {
    token = value;
    restored = true;
    try {
      storage()?.setItem(KEY, value);
    } catch {
      /* memory-only is fine */
    }
  },

  clear(): void {
    token = null;
    restored = true;
    try {
      storage()?.removeItem(KEY);
    } catch {
      /* nothing to clear */
    }
  },

  has(): boolean {
    return this.get() !== null;
  },
};
