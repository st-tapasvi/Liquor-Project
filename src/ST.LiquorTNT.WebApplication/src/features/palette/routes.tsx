import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the palette module. `module: 'palette'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const paletteRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.palette.list,
    page: () => import('./PalettePage'),
    permission: ['palette.view', 'palette.manage'],
    module: 'palette',
  }),
];
