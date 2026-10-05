import { queryClient } from '../api/query-client';

import { useTenantStore } from './tenant.store';

const CONTEXT = {
  companies: [
    { companyId: 1, companyName: 'Alpha', exciseCode: 'UP' },
    { companyId: 2, companyName: 'Beta', exciseCode: 'MP' },
  ],
  plants: [
    { plantId: 11, plantName: 'Unit I', companyId: 1 },
    { plantId: 21, plantName: 'Unit II', companyId: 2 },
  ],
  companyId: 1,
  plantId: 11,
};

describe('tenant store', () => {
  beforeEach(() => {
    useTenantStore.getState().setContext(CONTEXT);
  });

  it('resolves the company, its excise and its plant from the context', () => {
    const { companyName, exciseCode, plantName, loaded } = useTenantStore.getState();

    expect(loaded).toBe(true);
    expect(companyName).toBe('Alpha');
    expect(exciseCode).toBe('UP');
    expect(plantName).toBe('Unit I');
  });

  it('clears every cached query when the company changes', () => {
    queryClient.setQueryData(['users', 1, 'list'], [{ id: 7 }]);
    expect(queryClient.getQueryData(['users', 1, 'list'])).toBeDefined();

    useTenantStore.getState().selectCompany(2);

    // Company 1's rows must not survive into company 2's session (§9).
    expect(queryClient.getQueryData(['users', 1, 'list'])).toBeUndefined();
    expect(useTenantStore.getState().exciseCode).toBe('MP');
  });

  it('drops the selected plant on a company switch, because a plant belongs to one company', () => {
    useTenantStore.getState().selectCompany(2);

    expect(useTenantStore.getState().plantId).toBeNull();
    expect(useTenantStore.getState().plantName).toBeNull();
  });

  it('does not clear the cache when the same company is re-selected', () => {
    queryClient.setQueryData(['users', 1, 'list'], [{ id: 7 }]);

    useTenantStore.getState().selectCompany(1);

    expect(queryClient.getQueryData(['users', 1, 'list'])).toBeDefined();
  });

  it('refuses a plant that belongs to another company', () => {
    useTenantStore.getState().selectPlant(21);

    expect(useTenantStore.getState().plantId).toBe(11);
  });
});
