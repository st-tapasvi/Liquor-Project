import { useSessionStore } from '@/core/auth';
import { PATHS } from '@/core/router';

import { mockAuthState } from '@/test/msw/handlers';
import { renderWithProviders, screen, userEvent, waitFor, within } from '@/test/render';

import { LoginForm } from './LoginForm';

/** Signs in as the account that is already on two devices. */
async function submitAsFullAccount() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText(/user name/i), 'full');
  await user.type(screen.getByLabelText(/^password/i), 'Admin@123');
  await user.click(screen.getByRole('button', { name: /sign in/i }));
  return user;
}

describe('LoginForm', () => {
  it('signs the user in', async () => {
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'admin');
    await user.type(screen.getByLabelText(/^password/i), 'Admin@123');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => expect(useSessionStore.getState().status).toBe('authenticated'));
    expect(useSessionStore.getState().user?.userName).toBe('admin');
    expect(useSessionStore.getState().expiresAt).toBe('2026-10-01T09:00:00');
  });

  it('shows the API message on wrong credentials and clears the password', async () => {
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'admin');
    await user.type(screen.getByLabelText(/^password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/incorrect/i);
    expect(screen.getByLabelText(/^password/i)).toHaveValue('');
    expect(useSessionStore.getState().status).toBe('anonymous');
  });

  it('shows the lock detail from the API', async () => {
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'locked');
    await user.type(screen.getByLabelText(/^password/i), 'whatever');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again after/i);
  });

  it('sends a user with a temporary password to the change-password screen', async () => {
    const { router } = renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'temp');
    await user.type(screen.getByLabelText(/^password/i), 'Temp@12345');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => expect(router.state.location.pathname).toBe(PATHS.changePassword));
  });

  describe('when the account is signed in on too many devices', () => {
    it('lists the devices instead of a bare refusal', async () => {
      renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
      await submitAsFullAccount();

      const panel = await screen.findByRole('region', { name: /signed-in devices/i });
      expect(panel).toHaveTextContent(/maximum active sessions reached/i);
      expect(within(panel).getByText(/chrome on windows/i)).toBeInTheDocument();
      expect(within(panel).getByText(/safari on ios/i)).toBeInTheDocument();
      expect(within(panel).getAllByRole('button', { name: /sign out and continue/i })).toHaveLength(2);
      expect(useSessionStore.getState().status).toBe('anonymous');
    });

    it('signs the chosen device out and completes the login', async () => {
      renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
      const user = await submitAsFullAccount();

      const panel = await screen.findByRole('region', { name: /signed-in devices/i });
      await user.click(within(panel).getAllByRole('button', { name: /sign out and continue/i })[0]!);

      await waitFor(() => expect(useSessionStore.getState().status).toBe('authenticated'));
      expect(mockAuthState.openSessions.map((s) => s.id)).toEqual([72]);
      expect(screen.queryByRole('region', { name: /signed-in devices/i })).not.toBeInTheDocument();
    });

    it('keeps the list read-only when the server does not allow ending a session', async () => {
      mockAuthState.canEndOtherSession = false;
      renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
      await submitAsFullAccount();

      const panel = await screen.findByRole('region', { name: /signed-in devices/i });
      expect(panel).toHaveTextContent(/log out on one of them/i);
      expect(within(panel).queryByRole('button', { name: /sign out and continue/i })).not.toBeInTheDocument();
    });
  });

  it('validates required fields before calling the API', async () => {
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    await userEvent.setup().click(screen.getByRole('button', { name: /sign in/i }));
    expect(await screen.findByText(/user name is required/i)).toBeInTheDocument();
  });
});
