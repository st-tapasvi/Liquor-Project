import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClientProvider } from '@tanstack/react-query';
import { render, type RenderOptions } from '@testing-library/react';
import type { ReactElement, ReactNode } from 'react';
import { createMemoryRouter, RouterProvider } from 'react-router';

import { createQueryClient, type CurrentUserResponse } from '@/core/api';
import { useSessionStore } from '@/core/auth';
import { theme } from '@/core/theme';

import { ConfirmProvider, SnackbarProvider } from '@/shared/hooks';

import { makeCurrentUser } from './factories/user.factory';

interface RenderWithProvidersOptions extends Omit<RenderOptions, 'wrapper'> {
  /** Initial URL of the memory router. */
  route?: string;
  /** Path pattern of the rendered element (for `useParams`). Defaults to `route`. */
  path?: string;
  /** Logged-in user; pass `null` for an anonymous session. Defaults to the admin. */
  user?: Partial<CurrentUserResponse> | null;
}

/**
 * The ONLY render used in component tests. Provides theme, query client, router, snackbar, confirm
 * and a session. Tests assert on what the user sees and mock only the API function they exercise.
 */
export function renderWithProviders(
  ui: ReactElement,
  { route = '/', path, user, ...options }: RenderWithProvidersOptions = {},
) {
  if (user === null) useSessionStore.getState().setAnonymous(null);
  else
    useSessionStore.getState().setAuthenticated(makeCurrentUser(user ?? {}), {
      expiresAt: '2026-10-01T09:00:00' as never,
      idleTimeoutMinutes: 60,
    });

  const queryClient = createQueryClient();
  const router = createMemoryRouter(
    [
      { path: path ?? route, element: ui },
      { path: '*', element: <div>route: fallback</div> },
    ],
    {
      initialEntries: [route],
    },
  );

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <QueryClientProvider client={queryClient}>
          <SnackbarProvider>
            <ConfirmProvider>{children}</ConfirmProvider>
          </SnackbarProvider>
        </QueryClientProvider>
      </ThemeProvider>
    );
  }

  return { ...render(<RouterProvider router={router} />, { wrapper: Wrapper, ...options }), router, queryClient };
}

export { screen, waitFor, within } from '@testing-library/react';
export { default as userEvent } from '@testing-library/user-event';
