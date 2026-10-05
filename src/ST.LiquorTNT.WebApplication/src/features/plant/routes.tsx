import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the plant module. `module: 'plant'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const plantRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.plant.list,
    page: () => import('./PlantPage'),
    permission: ['plant.view', 'plant.manage'],
    module: 'plant',
  }),
];
