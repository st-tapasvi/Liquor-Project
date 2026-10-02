import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the brands module. `module: 'brands'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const brandRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.brands.list,
    page: () => import('./BrandPage'),
    permission: ['brands.view', 'brands.manage'],
    module: 'brands',
  }),
];
