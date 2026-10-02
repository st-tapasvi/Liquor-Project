import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the batches module. `module: 'batches'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const batchRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.batches.list,
    page: () => import('./BatchPage'),
    permission: ['batches.view', 'batches.manage'],
    module: 'batches',
  }),
];
