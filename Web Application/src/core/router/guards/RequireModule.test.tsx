import { renderWithProviders, screen } from '@/test/render';

import { useModuleFlagsStore } from '../../modules';

import { RequireModule } from './RequireModule';

describe('RequireModule', () => {
  it('renders the screen when the installation has the module', () => {
    useModuleFlagsStore.getState().setFlags(['dispatch']);

    renderWithProviders(
      <RequireModule module="dispatch">
        <div>dispatch screen</div>
      </RequireModule>,
      { route: '/dispatch' },
    );

    expect(screen.getByText('dispatch screen')).toBeInTheDocument();
  });

  it('answers page-not-found for a module this installation does not have', () => {
    useModuleFlagsStore.getState().setFlags([]);

    renderWithProviders(
      <RequireModule module="dispatch">
        <div>dispatch screen</div>
      </RequireModule>,
      { route: '/dispatch' },
    );

    // §8.2: unreachable, not merely disabled — the address behaves as if it does not exist.
    expect(screen.queryByText('dispatch screen')).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /page not found/i })).toBeInTheDocument();
  });

  it('never hides an always-present module, even when no flags were received', () => {
    // The flag request failed; the core platform must still work.
    useModuleFlagsStore.getState().setFlags([]);

    renderWithProviders(
      <RequireModule module="brands">
        <div>brand screen</div>
      </RequireModule>,
      { route: '/brands' },
    );

    expect(screen.getByText('brand screen')).toBeInTheDocument();
  });

  it('waits rather than reporting a module missing while the flags are still loading', () => {
    useModuleFlagsStore.setState({ loaded: false, enabled: new Set() });

    renderWithProviders(
      <RequireModule module="dispatch">
        <div>dispatch screen</div>
      </RequireModule>,
      { route: '/dispatch' },
    );

    expect(screen.queryByRole('heading', { name: /page not found/i })).not.toBeInTheDocument();
    expect(screen.queryByText('dispatch screen')).not.toBeInTheDocument();
  });
});
