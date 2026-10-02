import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

/**
 * Routes of the case-data module. `module: 'case-data'` makes the route resolve to page-not-found when the
 * installation does not have this module (specification §8.2).
 */
export const caseDataRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.caseData.search,
    page: () => import('./CaseDataPage'),
    permission: ['case-data.view', 'case-data.export'],
    module: 'case-data',
  }),
];
