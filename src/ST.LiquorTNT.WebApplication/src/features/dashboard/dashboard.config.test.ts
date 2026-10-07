import { toPermissionSet } from '@/core/auth';

import { rightsLabel } from './dashboard.config';

describe('rightsLabel', () => {
  it('lists only the rights of the given module', () => {
    const rights = toPermissionSet(['brands.view', 'brands.sync', 'users.view', 'brands-x.view']);
    expect(rightsLabel('brands', rights)).toBe('view · sync');
    expect(rightsLabel('batches', rights)).toBe('');
  });
});
