import { PATHS, safeRedirectPath } from './paths';

describe('safeRedirectPath', () => {
  it('accepts a same-origin absolute path', () => {
    expect(safeRedirectPath('/users/7/edit?x=1')).toBe('/users/7/edit?x=1');
  });

  it('falls back on open-redirect attempts', () => {
    expect(safeRedirectPath('//evil.example')).toBe(PATHS.dashboard);
    expect(safeRedirectPath('https://evil.example')).toBe(PATHS.dashboard);
    expect(safeRedirectPath('/\\evil.example')).toBe(PATHS.dashboard);
    expect(safeRedirectPath(['java', 'script:alert(1)'].join(''))).toBe(PATHS.dashboard);
    expect(safeRedirectPath('/users\u0000')).toBe(PATHS.dashboard);
  });

  it('never redirects back to login', () => {
    expect(safeRedirectPath('/login')).toBe(PATHS.dashboard);
    expect(safeRedirectPath(null)).toBe(PATHS.dashboard);
  });
});
