import type { RouteObject } from 'react-router';

import { createProtectedRoute, PATHS } from '@/core/router';

export const companyRoutes: RouteObject[] = [
  createProtectedRoute({
    path: PATHS.company.list,
    page: () => import('./CompanyPage'),
    permission: 'company.view',
  }),
  createProtectedRoute({
    path: PATHS.company.supplierCodes,
    page: () => import('./SupplierCodePage'),
    permission: 'suppliercode.view',
  }),
  createProtectedRoute({
    path: PATHS.company.liquorCategories,
    page: () => import('./LiquorCategoryPage'),
    permission: 'liquorcategory.view',
  }),
];
