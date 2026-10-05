import { redact } from './redact';

describe('redact', () => {
  it('masks credential-like keys at any depth', () => {
    const out = redact({ userName: 'ravi', password: 'x', nested: { accessToken: 'y', jwt: 'z', ok: 1 } }) as Record<
      string,
      unknown
    >;
    expect(out['userName']).toBe('ravi');
    expect(out['password']).toBe('***');
    expect((out['nested'] as Record<string, unknown>)['accessToken']).toBe('***');
    expect((out['nested'] as Record<string, unknown>)['jwt']).toBe('***');
    expect((out['nested'] as Record<string, unknown>)['ok']).toBe(1);
  });

  it('reduces errors to name and message', () => {
    const out = redact(new TypeError('boom')) as Record<string, unknown>;
    expect(out).toEqual({ name: 'TypeError', message: 'boom' });
  });

  it('truncates long strings and bounds depth', () => {
    expect((redact('a'.repeat(600)) as string).length).toBeLessThan(600);
    const deep = { a: { b: { c: { d: { e: { f: 1 } } } } } };
    expect(JSON.stringify(redact(deep))).toContain('[depth]');
  });
});
