import type { UserResponse } from '@/core/api';
import { PATHS } from '@/core/router';

import { makeUser } from '@/test/factories/user.factory';
import { renderWithProviders, screen, userEvent, waitFor, within } from '@/test/render';

import { usersApi } from '../api/users.api';

import UserListPage from './UserListPage';

let users: UserResponse[];

beforeEach(() => {
  users = [
    makeUser({ id: 10, userName: 'ravi.k' }),
    makeUser({ id: 11, userName: 'locked.user', lockedUntil: '2026-10-01T09:00:00' as never }),
    makeUser({ id: 12, userName: 'inactive.user', isActive: false }),
  ];

  vi.spyOn(usersApi, 'list').mockImplementation((params) => {
    const search = params.search?.toLowerCase() ?? '';
    const items = users.filter(
      (user) => user.userName.toLowerCase().includes(search) || user.fullName?.toLowerCase().includes(search),
    );
    return Promise.resolve({
      items,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 50,
      totalCount: items.length,
    });
  });

  vi.spyOn(usersApi, 'unlock').mockImplementation((id) => {
    const index = users.findIndex((user) => user.id === id);
    if (index < 0) return Promise.reject(new Error('User not found.'));
    const updated = { ...users[index]!, lockedUntil: null, isBlocked: false };
    users[index] = updated;
    return Promise.resolve(updated);
  });
});

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
