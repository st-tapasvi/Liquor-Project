import { renderWithProviders, screen, waitFor } from '@/test/render';

import { PATHS } from '../paths';

import { RequireAnonymous } from './RequireAnonymous';
import { RequireAuth } from './RequireAuth';
import { RequirePermission } from './RequirePermission';

describe('router guards', () => {
  it('RequireAuth sends an anonymous visitor to login and remembers where they were', async () => {
    const { router } = renderWithProviders(
      <RequireAuth>
        <div>secret</div>
      </RequireAuth>,
      { route: '/users?page=2', path: '/users', user: null },
    );
    await waitFor(() => expect(router.state.location.pathname).toBe(PATHS.login));
    expect(router.state.location.state).toEqual({ from: '/users?page=2' });
    expect(screen.queryByText('secret')).not.toBeInTheDocument();
  });

  it('RequireAnonymous sends a signed-in user on to the dashboard', async () => {
    const { router } = renderWithProviders(
      <RequireAnonymous>
        <div>login form</div>
      </RequireAnonymous>,
      { route: PATHS.login },
    );
    await waitFor(() => expect(router.state.location.pathname).toBe(PATHS.dashboard));
  });

  it('RequirePermission shows Forbidden to a user without the right', async () => {
    const { router } = renderWithProviders(
      <RequirePermission right="settings.manage">
        <div>admin only</div>
      </RequirePermission>,
      { route: PATHS.settings.security, user: { permissions: ['users.view'] } },
    );
    await waitFor(() => expect(router.state.location.pathname).toBe(PATHS.forbidden));
    expect(screen.queryByText('admin only')).not.toBeInTheDocument();
  });

  it('RequirePermission renders for a user with the right', () => {
    renderWithProviders(
      <RequirePermission right={['settings.view', 'settings.manage']}>
        <div>admin only</div>
      </RequirePermission>,
      { route: PATHS.settings.security, user: { permissions: ['settings.view'] } },
    );
    expect(screen.getByText('admin only')).toBeInTheDocument();
  });
});
