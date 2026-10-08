import { type ComponentType, lazy, Suspense } from 'react';
import type { RouteObject } from 'react-router';

import type { PermissionKey } from '../auth/permissions';

import { RequirePermission } from './guards/RequirePermission';
import { LoadingFallback } from './LoadingFallback';
import { RouteErrorFallback } from './RouteErrorFallback';

type PageLoader = () => Promise<{ default: ComponentType }>;

interface ProtectedRouteOptions {
  path: string;
  page: PageLoader;
  permission?: PermissionKey | readonly PermissionKey[];
  children?: RouteObject[];
}

const RETRY_DELAY_MS = 600;

function withRetry(loader: PageLoader): PageLoader {
  return () =>
    loader().catch(
      () =>
        new Promise<{ default: ComponentType }>((resolve, reject) => {
          setTimeout(() => {
            loader().then(resolve, reject);
          }, RETRY_DELAY_MS);
        }),
    );
}

function lazyPage(loader: PageLoader) {
  const Page = lazy(withRetry(loader));
  return (
    <Suspense fallback={<LoadingFallback />}>
      <Page />
    </Suspense>
  );
}

export function createProtectedRoute({ path, page, permission, children }: ProtectedRouteOptions): RouteObject {
  let element = lazyPage(page);
  if (permission) element = <RequirePermission right={permission}>{element}</RequirePermission>;
  return { path, element, errorElement: <RouteErrorFallback />, ...(children ? { children } : {}) };
}

export function createPublicRoute({ path, page }: Pick<ProtectedRouteOptions, 'path' | 'page'>): RouteObject {
  return { path, element: lazyPage(page), errorElement: <RouteErrorFallback /> };
}
