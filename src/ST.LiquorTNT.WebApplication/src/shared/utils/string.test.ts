import { emptyToNull, initials, nullToEmpty } from './string';

describe('string utils', () => {
  it('emptyToNull trims and nulls blanks', () => {
    expect(emptyToNull('  ')).toBeNull();
    expect(emptyToNull(' a ')).toBe('a');
    expect(emptyToNull(undefined)).toBeNull();
  });

  it('nullToEmpty', () => {
    expect(nullToEmpty(null)).toBe('');
    expect(nullToEmpty('x')).toBe('x');
  });

  it('initials', () => {
    expect(initials('Ravi Kumar')).toBe('RK');
    expect(initials('admin')).toBe('A');
    expect(initials('')).toBe('?');
  });
});
