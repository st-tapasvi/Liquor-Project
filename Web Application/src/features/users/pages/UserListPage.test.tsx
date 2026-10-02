import { PATHS } from '@/core/router';

import { renderWithProviders, screen, userEvent, waitFor, within } from '@/test/render';

import UserListPage from './UserListPage';

describe('UserListPage', () => {
  it('lists users from the API with their status', async () => {
    renderWithProviders(<UserListPage />, { route: PATHS.users.list });

    expect(await screen.findByText('ravi.k')).toBeInTheDocument();
    expect(screen.getByText('Locked')).toBeInTheDocument();
    expect(screen.getByText('Inactive')).toBeInTheDocument();
  });

  it('shows the New user button only to users with the manage right', async () => {
    renderWithProviders(<UserListPage />, { route: PATHS.users.list, user: { permissions: ['users.view'] } });
    await screen.findByText('ravi.k');
    expect(screen.queryByRole('link', { name: /new user/i })).not.toBeInTheDocument();
  });

  it('filters by search text through the URL', async () => {
    const { router } = renderWithProviders(<UserListPage />, { route: PATHS.users.list });
    await screen.findByText('ravi.k');

    await userEvent.setup().type(screen.getByLabelText(/search users/i), 'inactive');

    await waitFor(() => expect(router.state.location.search).toContain('search=inactive'));
    await waitFor(() => expect(screen.queryByText('ravi.k')).not.toBeInTheDocument());
    expect(screen.getByText('inactive.user')).toBeInTheDocument();
  });

  it('unlocks a locked user and refreshes the row', async () => {
    renderWithProviders(<UserListPage />, { route: PATHS.users.list });
    const row = (await screen.findByText('locked.user')).closest('[role="row"]');
    expect(row).not.toBeNull();

    await userEvent.setup().click(within(row as HTMLElement).getByRole('button', { name: /unlock locked.user/i }));

    await waitFor(() => expect(within(row as HTMLElement).queryByText('Locked')).not.toBeInTheDocument());
    expect(await screen.findByText(/unlocked/i)).toBeInTheDocument();
  });
});
