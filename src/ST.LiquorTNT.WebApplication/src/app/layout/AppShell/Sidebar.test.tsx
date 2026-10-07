import { useModuleFlagsStore } from '@/core/modules';

import { renderWithProviders, screen, userEvent, within } from '@/test/render';

import { Sidebar } from './Sidebar';

type RenderOptions = NonNullable<Parameters<typeof renderWithProviders>[1]>;

function renderSidebar(user?: RenderOptions['user'], open = true) {
  return renderWithProviders(<Sidebar variant="permanent" open={open} onClose={() => undefined} />, {
    route: '/dashboard',
    ...(user === undefined ? {} : { user }),
  });
}

describe('Sidebar', () => {
  it('lists the modules the admin may open, and hides modules this installation does not have', () => {
    useModuleFlagsStore.getState().setFlags([]);
    renderSidebar();
    const nav = screen.getByRole('navigation', { name: /main navigation/i });

    for (const label of ['Dashboard', 'Company', 'Users', 'Brands', 'Batches', 'Case Data', 'Portal Sync', 'Reports']) {
      expect(within(nav).getByRole('link', { name: label })).toBeInTheDocument();
    }
    expect(within(nav).queryByRole('link', { name: 'Dispatch' })).not.toBeInTheDocument();
    expect(within(nav).getByRole('link', { name: 'Dashboard' })).toHaveClass('active');
  });

  it('shows a flagged module once the installation enables it', () => {
    useModuleFlagsStore.getState().setFlags(['dispatch']);
    renderSidebar();
    expect(screen.getByRole('link', { name: 'Dispatch' })).toBeInTheDocument();
  });

  it('gives a user without rights only what everyone may open', async () => {
    useModuleFlagsStore.getState().setFlags([]);
    renderSidebar({ isAdministrator: false, permissions: ['dashboard.view', 'sessions.manage'] });
    const nav = screen.getByRole('navigation', { name: /main navigation/i });

    expect(within(nav).queryByRole('link', { name: 'Users' })).not.toBeInTheDocument();
    await userEvent.setup().click(within(nav).getByRole('button', { name: /settings/i }));
    expect(within(nav).getByRole('link', { name: 'My sessions' })).toBeInTheDocument();
    expect(within(nav).queryByRole('link', { name: 'Security settings' })).not.toBeInTheDocument();
  });

  it('collapses to an icon rail that still links every module', () => {
    useModuleFlagsStore.getState().setFlags([]);
    renderSidebar(undefined, false);
    const nav = screen.getByRole('navigation', { name: /main navigation/i });
    expect(within(nav).getByRole('link', { name: 'Users' })).toBeInTheDocument();
    expect(within(nav).getByRole('button', { name: /settings/i })).toHaveAttribute('aria-expanded', 'false');
  });
});
