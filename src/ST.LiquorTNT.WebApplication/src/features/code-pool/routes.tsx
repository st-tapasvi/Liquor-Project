import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the code-pool module. `module: 'code-pool'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const codePoolRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.codePool.list,
    page: () => import('./CodePoolPage'),
    permission: ['code-pool.view', 'code-pool.download'],
    module: 'code-pool',
  }),
];
