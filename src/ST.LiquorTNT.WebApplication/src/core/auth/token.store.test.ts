import { tokenStore } from './token.store';

afterEach(() => tokenStore.clear());

describe('tokenStore', () => {
  it('starts empty', () => {
    expect(tokenStore.get()).toBeNull();
    expect(tokenStore.has()).toBe(false);
  });

  it('keeps the token for the tab (survives a reload) and forgets it on clear', () => {
    tokenStore.set('eyJ.a.b');
    expect(tokenStore.get()).toBe('eyJ.a.b');
    // eslint-disable-next-line no-restricted-globals -- asserting the one sanctioned mirror
    expect(sessionStorage.getItem('st.tnt.access-token')).toBe('eyJ.a.b');

    tokenStore.clear();
    expect(tokenStore.get()).toBeNull();
    // eslint-disable-next-line no-restricted-globals -- asserting the one sanctioned mirror
    expect(sessionStorage.getItem('st.tnt.access-token')).toBeNull();
  });
});
