import { AxiosError, AxiosHeaders, type AxiosAdapter, type AxiosResponse } from 'axios';

import { reauthStore, useReauthStore } from '../auth/reauth.store';
import { sessionStore, useSessionStore } from '../auth/session.store';
import { tokenStore } from '../auth/token.store';
import { ApiError, NetworkError, ValidationError } from '../errors';

import { http } from './http';

const originalAdapter = http.defaults.adapter;

function reply(config: Parameters<AxiosAdapter>[0], data: unknown, status = 200): AxiosResponse {
  return {
    data,
    status,
    statusText: String(status),
    headers: new AxiosHeaders({ 'x-correlation-id': 'test-correlation-id' }),
    config,
    request: {},
  };
}

function failure(config: Parameters<AxiosAdapter>[0], status: number, data: unknown): AxiosError {
  const response = reply(config, data, status);
  return new AxiosError('Request failed', AxiosError.ERR_BAD_RESPONSE, config, undefined, response);
}

function problem(status: number, errorCode: string, title: string, errors?: Record<string, string[]>): unknown {
  return {
    status,
    errorCode,
    title,
    correlationId: 'test-correlation-id',
    ...(errors ? { errors } : {}),
  };
}

afterEach(() => {
  if (originalAdapter === undefined) delete http.defaults.adapter;
  else http.defaults.adapter = originalAdapter;
  tokenStore.clear();
});

describe('http client', () => {
  it('sends the CSRF header and a correlation id on every request', async () => {
    let seen: AxiosHeaders | undefined;
    http.defaults.adapter = (config) => {
      seen = config.headers;
      return Promise.resolve(reply(config, { ok: true }));
    };
    await http.get('/ping');
    expect(seen?.get('x-requested-with')).toBe('XMLHttpRequest');
    expect(seen?.get('x-correlation-id')).toMatch(/^[0-9a-f]{32}$/);
  });

  it('sends the bearer token once the session has one, and nothing before', async () => {
    let seen: AxiosHeaders | undefined;
    http.defaults.adapter = (config) => {
      seen = config.headers;
      return Promise.resolve(reply(config, { ok: true }));
    };

    await http.get('/anonymous');
    expect(seen?.has('Authorization')).toBe(false);

    tokenStore.set('eyJ.test.token');
    await http.get('/secure');
    expect(seen?.get('Authorization')).toBe('Bearer eyJ.test.token');
  });

  it('forgets the token when the session ends', () => {
    tokenStore.set('eyJ.test.token');
    sessionStore.setAnonymous('logout');
    expect(tokenStore.has()).toBe(false);
  });

  it('maps 400 VALIDATION_FAILED to ValidationError', async () => {
    http.defaults.adapter = (config) =>
      Promise.reject(failure(config, 400, problem(400, 'VALIDATION_FAILED', 'Bad', { name: ['Required.'] })));
    await expect(http.post('/thing', {})).rejects.toBeInstanceOf(ValidationError);
  });

  it('maps a 404 to ApiError with the code and correlation id', async () => {
    http.defaults.adapter = (config) => Promise.reject(failure(config, 404, problem(404, 'NOT_FOUND', 'Not found.')));
    const err = await http.get('/thing/1').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).code).toBe('NOT_FOUND');
    expect((err as ApiError).correlationId).toBe('test-correlation-id');
  });

  it('turns a connection failure into NetworkError', async () => {
    http.defaults.adapter = (config) => Promise.reject(new AxiosError('Network Error', AxiosError.ERR_NETWORK, config));
    await expect(http.get('/down')).rejects.toBeInstanceOf(NetworkError);
  });

  it('ends the session on 401 SESSION_TIMED_OUT', async () => {
    useSessionStore.setState({ status: 'authenticated' });
    http.defaults.adapter = (config) =>
      Promise.reject(failure(config, 401, problem(401, 'SESSION_TIMED_OUT', 'Idle.')));
    await expect(http.get('/secure')).rejects.toBeInstanceOf(ApiError);
    expect(sessionStore.get().status).toBe('anonymous');
    expect(sessionStore.get().endReason).toBe('timed_out');
  });

  it('does not end the session for a silent 401 (boot-time /me)', async () => {
    useSessionStore.setState({ status: 'authenticated' });
    http.defaults.adapter = (config) => Promise.reject(failure(config, 401, problem(401, 'UNAUTHENTICATED', 'No.')));
    await expect(http.get('/secure', { meta: { silentUnauthorized: true } })).rejects.toBeInstanceOf(ApiError);
    expect(sessionStore.get().status).toBe('authenticated');
  });

  it('parks a SESSION_EXPIRED request, then retries it after re-authentication', async () => {
    let calls = 0;
    http.defaults.adapter = (config) => {
      calls += 1;
      if (calls === 1) return Promise.reject(failure(config, 401, problem(401, 'SESSION_EXPIRED', 'Hard limit.')));
      return Promise.resolve(reply(config, { ok: true }));
    };

    const pending = http.get<{ ok: boolean }>('/secure');
    await vi.waitFor(() => expect(useReauthStore.getState().isOpen).toBe(true));

    reauthStore.resolve();
    const response = await pending;
    expect(response.data.ok).toBe(true);
    expect(calls).toBe(2);
    expect(useReauthStore.getState().isOpen).toBe(false);
  });

  it('fails the parked request when re-authentication is cancelled', async () => {
    http.defaults.adapter = (config) =>
      Promise.reject(failure(config, 401, problem(401, 'SESSION_EXPIRED', 'Hard limit.')));
    const pending = http.get('/secure');
    await vi.waitFor(() => expect(useReauthStore.getState().isOpen).toBe(true));
    reauthStore.reject(new Error('cancelled'));
    await expect(pending).rejects.toThrow('cancelled');
  });
});
