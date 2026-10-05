import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the company module. `module: 'company'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const companyRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.company.list,
    page: () => import('./CompanyPage'),
    permission: ['company.view', 'company.manage'],
    module: 'company',
  }),
];
