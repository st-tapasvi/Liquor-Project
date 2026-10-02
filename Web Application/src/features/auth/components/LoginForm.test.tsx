import type { ActiveSessionSummary, IstDateTime, LoginResponse } from '@/core/api';
import { authApi, useSessionStore } from '@/core/auth';
import { ApiError, SessionLimitError } from '@/core/errors';
import { PATHS } from '@/core/router';

import { makeCurrentUser } from '@/test/factories/user.factory';
import { renderWithProviders, screen, userEvent, waitFor, within } from '@/test/render';

import { LoginForm } from './LoginForm';

const LOGIN_RESPONSE: LoginResponse = {
  expiresAt: '2026-10-01T09:00:00' as IstDateTime,
  idleTimeoutMinutes: 60,
  user: makeCurrentUser(),
};

const OPEN_SESSIONS: readonly ActiveSessionSummary[] = [
  {
    id: 71,
    loginAt: '2026-09-28T08:00:00' as IstDateTime,
    lastActivityAt: '2026-09-28T09:30:00' as IstDateTime,
    expiresAt: '2026-09-29T08:00:00' as IstDateTime,
    ipAddress: '192.168.1.20',
    userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/141.0.0.0 Safari/537.36',
  },
  {
    id: 72,
    loginAt: '2026-09-27T17:45:00' as IstDateTime,
    lastActivityAt: null,
    expiresAt: '2026-09-28T17:45:00' as IstDateTime,
    ipAddress: '192.168.1.55',
    userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) Safari/605.1.15',
  },
];

function sessionLimit(canEndOther: boolean): SessionLimitError {
  return new SessionLimitError({
    status: 409,
    code: 'SESSION_LIMIT_REACHED',
    title: 'Maximum active sessions reached.',
    detail: canEndOther
      ? 'Choose a signed-in device to end, or log out there first.'
      : 'Log out on one of the signed-in devices first.',
    sessions: OPEN_SESSIONS,
    canEndOther,
  });
}

/** Signs in as the account that is already on two devices. */
async function submitAsFullAccount() {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText(/user name/i), 'full');
  await user.type(screen.getByLabelText(/^password/i), 'Admin@123');
  await user.click(screen.getByRole('button', { name: /log in/i }));
  return user;
}

describe('LoginForm', () => {
  it('signs the user in', async () => {
    vi.spyOn(authApi, 'login').mockResolvedValue(LOGIN_RESPONSE);
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'admin');
    await user.type(screen.getByLabelText(/^password/i), 'Admin@123');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => expect(useSessionStore.getState().status).toBe('authenticated'));
    expect(useSessionStore.getState().user?.userName).toBe('admin');
    expect(useSessionStore.getState().expiresAt).toBe('2026-10-01T09:00:00');
  });

  it('shows the API message on wrong credentials and clears the password', async () => {
    vi.spyOn(authApi, 'login').mockRejectedValue(
      new ApiError({
        status: 401,
        code: 'INVALID_CREDENTIALS',
        title: 'User name or password is incorrect.',
      }),
    );
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'admin');
    await user.type(screen.getByLabelText(/^password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/incorrect/i);
    expect(screen.getByLabelText(/^password/i)).toHaveValue('');
    expect(useSessionStore.getState().status).toBe('anonymous');
  });

  it('shows the lock detail from the API', async () => {
    vi.spyOn(authApi, 'login').mockRejectedValue(
      new ApiError({
        status: 403,
        code: 'USER_LOCKED',
        title: 'This account is temporarily locked.',
        detail: 'Try again after 10:07.',
      }),
    );
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'locked');
    await user.type(screen.getByLabelText(/^password/i), 'whatever');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/try again after/i);
  });

  it('sends a user with a temporary password to the change-password screen', async () => {
    vi.spyOn(authApi, 'login').mockRejectedValue(
      new ApiError({
        status: 403,
        code: 'PASSWORD_CHANGE_REQUIRED',
        title: 'A new password must be set.',
      }),
    );
    const { router } = renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    const user = userEvent.setup();

    await user.type(screen.getByLabelText(/user name/i), 'temp');
    await user.type(screen.getByLabelText(/^password/i), 'Temp@12345');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => expect(router.state.location.pathname).toBe(PATHS.changePassword));
  });

  describe('when the account is signed in on too many devices', () => {
    it('lists the devices instead of a bare refusal', async () => {
      vi.spyOn(authApi, 'login').mockRejectedValue(sessionLimit(true));
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
      const login = vi
        .spyOn(authApi, 'login')
        .mockRejectedValueOnce(sessionLimit(true))
        .mockResolvedValueOnce(LOGIN_RESPONSE);
      renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
      const user = await submitAsFullAccount();

      const panel = await screen.findByRole('region', { name: /signed-in devices/i });
      await user.click(within(panel).getAllByRole('button', { name: /sign out and continue/i })[0]!);

      await waitFor(() => expect(useSessionStore.getState().status).toBe('authenticated'));
      expect(login).toHaveBeenLastCalledWith({ userName: 'full', password: 'Admin@123', endSessionId: 71 });
      expect(screen.queryByRole('region', { name: /signed-in devices/i })).not.toBeInTheDocument();
    });

    it('keeps the list read-only when the server does not allow ending a session', async () => {
      vi.spyOn(authApi, 'login').mockRejectedValue(sessionLimit(false));
      renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
      await submitAsFullAccount();

      const panel = await screen.findByRole('region', { name: /signed-in devices/i });
      expect(panel).toHaveTextContent(/log out on one of them/i);
      expect(within(panel).queryByRole('button', { name: /sign out and continue/i })).not.toBeInTheDocument();
    });
  });

  it('validates required fields before calling the API', async () => {
    const login = vi.spyOn(authApi, 'login');
    renderWithProviders(<LoginForm />, { route: PATHS.login, user: null });
    await userEvent.setup().click(screen.getByRole('button', { name: /log in/i }));
    expect(await screen.findByText(/user name is required/i)).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });
});
