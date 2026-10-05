import { ApiError, ValidationError } from './app-error';
import { parseProblemDetails, toApiError } from './problem-details';

describe('problem details mapping', () => {
  it('maps a validation failure to ValidationError with field errors', () => {
    const err = toApiError(400, {
      status: 400,
      errorCode: 'VALIDATION_FAILED',
      title: 'Validation failed.',
      correlationId: 'abc',
      errors: { password: ['Too short.', 'Needs a number.'] },
    });
    expect(err).toBeInstanceOf(ValidationError);
    expect((err as ValidationError).errors['password']).toEqual(['Too short.', 'Needs a number.']);
    expect(err.correlationId).toBe('abc');
  });

  it('maps a known error code', () => {
    const err = toApiError(403, { errorCode: 'USER_LOCKED', title: 'Locked', detail: 'Try later.' });
    expect(err).toBeInstanceOf(ApiError);
    expect(err.code).toBe('USER_LOCKED');
    expect(err.message).toBe('Try later.');
    expect(err.is('USER_LOCKED')).toBe(true);
  });

  it('degrades gracefully when the body is not JSON (proxy error page)', () => {
    const err = toApiError(502, '<html>Bad gateway</html>', 'hdr-corr');
    expect(err.code).toBe('UNKNOWN');
    expect(err.status).toBe(502);
    expect(err.correlationId).toBe('hdr-corr');
  });

  it('ignores developer-only members and non-string messages', () => {
    const p = parseProblemDetails({ errorCode: 'UNEXPECTED_ERROR', stackTrace: ['x'], errors: { a: ['ok', 5] } });
    expect(p?.errors?.['a']).toEqual(['ok']);
    expect(p).not.toHaveProperty('stackTrace');
  });
});
