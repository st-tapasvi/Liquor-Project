/**
 * Every route path of the application, in one place. Components navigate with `PATHS.users.edit(id)`,
 * never with hand-written strings.
 */
export const PATHS = {
  root: '/',
  login: '/login',
  changePassword: '/change-password',
  forgotPassword: '/forgot-password',
  dashboard: '/dashboard',
  forbidden: '/forbidden',

  users: {
    list: '/users',
    new: '/users/new',
    edit: (id: string | number = ':id') => `/users/${id}/edit`,
  },

  // --- always-present modules ---
  company: { list: '/company' },
  brands: {
    list: '/brands',
    new: '/brands/new',
    edit: (id: string | number = ':id') => `/brands/${id}/edit`,
  },
  batches: {
    list: '/batches',
    new: '/batches/new',
    edit: (id: string | number = ':id') => `/batches/${id}/edit`,
  },
  caseData: {
    search: '/case-data',
    report: '/case-data/report',
  },
  portalSync: { list: '/portal-sync' },
  reports: {
    activity: '/reports/user-activity',
    dispatch: '/reports/dispatch',
    hologramWastage: '/reports/hologram-wastage',
  },
  license: { status: '/license' },

  // --- flag-controlled modules (absent from the menu and unreachable when switched off) ---
  plant: { list: '/plant' },
  plans: { list: '/plans' },
  codePool: { list: '/code-pool' },
  palette: { list: '/palette' },
  dispatch: {
    list: '/dispatch',
    xmlViewer: '/dispatch/xml-viewer',
  },
  outbox: { list: '/outbox' },

  settings: {
    security: '/settings/security',
    passwordPolicies: '/settings/password-policies',
    sessions: '/settings/sessions',
  },
} as const;

/**
 * Validates a post-login redirect target. Only same-origin, absolute-path targets are accepted, which
 * rules out open redirects (`//evil.example`, `https://…`, `javascript:`) by construction.
 */
export function safeRedirectPath(candidate: string | null | undefined, fallback: string = PATHS.dashboard): string {
  if (!candidate) return fallback;
  if (!candidate.startsWith('/') || candidate.startsWith('//') || candidate.startsWith('/\\')) return fallback;
  for (let i = 0; i < candidate.length; i += 1) {
    if (candidate.charCodeAt(i) < 0x20) return fallback; // control characters
  }
  if (candidate === PATHS.login || candidate.startsWith(`${PATHS.login}?`)) return fallback;
  return candidate;
}
