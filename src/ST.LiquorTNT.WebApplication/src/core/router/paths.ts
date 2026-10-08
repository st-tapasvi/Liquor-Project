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
    roles: '/roles',
  },

  company: {
    list: '/company',
    supplierCodes: '/company/supplier-codes',
    liquorCategories: '/company/liquor-categories',
  },

  settings: {
    security: '/settings/security',
    passwordPolicies: '/settings/password-policies',
    sessions: '/settings/sessions',
  },
} as const;


export function safeRedirectPath(candidate: string | null | undefined, fallback: string = PATHS.dashboard): string {
  if (!candidate) return fallback;
  if (!candidate.startsWith('/') || candidate.startsWith('//') || candidate.startsWith('/\\')) return fallback;
  for (let i = 0; i < candidate.length; i += 1) {
    if (candidate.charCodeAt(i) < 0x20) return fallback; // control characters
  }
  if (candidate === PATHS.login || candidate.startsWith(`${PATHS.login}?`)) return fallback;
  return candidate;
}
