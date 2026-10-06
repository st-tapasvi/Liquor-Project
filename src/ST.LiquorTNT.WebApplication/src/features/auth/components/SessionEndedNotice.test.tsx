import { useSessionStore } from '@/core/auth';
import { PATHS } from '@/core/router';

import { renderWithProviders, screen, waitFor } from '@/test/render';

import { SessionEndedNotice } from './SessionEndedNotice';

describe('SessionEndedNotice', () => {
  function renderWithReason(reason: Parameters<ReturnType<typeof useSessionStore.getState>['setAnonymous']>[0]) {
    renderWithProviders(<SessionEndedNotice />, { route: PATHS.login, user: null });
    useSessionStore.getState().setAnonymous(reason);
  }

  it('shows the logout confirmation as a toast and clears the reason', async () => {
    renderWithReason('logout');
    expect(await screen.findByText(/you have been logged out/i)).toBeInTheDocument();
    await waitFor(() => expect(useSessionStore.getState().endReason).toBeNull());
  });

  it('explains an idle time-out', async () => {
    renderWithReason('timed_out');
    expect(await screen.findByText(/ended because it was idle/i)).toBeInTheDocument();
  });

  it('leaves "unreachable" to the offline toast and keeps the reason for the re-check', async () => {
    renderWithReason('unreachable');
    await new Promise((r) => setTimeout(r, 20));
    expect(screen.queryByText(/could not be reached/i)).not.toBeInTheDocument();
    expect(useSessionStore.getState().endReason).toBe('unreachable');
  });
});
