import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the license module. `module: 'license'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const licenseRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.license.status,
    page: () => import('./LicensePage'),
    permission: ['license.view'],
    module: 'license',
  }),
];
