import { http as mswHttp, HttpResponse } from 'msw';

import { problem } from '@/test/msw/problem';
import { server } from '@/test/msw/server';

import { reauthStore, useReauthStore } from '../auth/reauth.store';
import { sessionStore, useSessionStore } from '../auth/session.store';
import { ApiError, NetworkError, ValidationError } from '../errors';

import { http } from './http';

describe('http client', () => {
  it('sends the CSRF header and a correlation id on every request', async () => {
    let seen: Headers | undefined;
    server.use(
      mswHttp.get('/api/ping', ({ request }) => {
        seen = request.headers;
        return HttpResponse.json({ ok: true });
      }),
    );
    await http.get('/ping');
    expect(seen?.get('x-requested-with')).toBe('XMLHttpRequest');
    expect(seen?.get('x-correlation-id')).toMatch(/^[0-9a-f]{32}$/);
  });

  it('maps 400 VALIDATION_FAILED to ValidationError', async () => {
    server.use(
      mswHttp.post('/api/thing', () => problem(400, 'VALIDATION_FAILED', 'Bad', { errors: { name: ['Required.'] } })),
    );
    await expect(http.post('/thing', {})).rejects.toBeInstanceOf(ValidationError);
  });

  it('maps a 404 to ApiError with the code and correlation id', async () => {
    server.use(mswHttp.get('/api/thing/1', () => problem(404, 'NOT_FOUND', 'Not found.')));
    const err = await http.get('/thing/1').catch((e: unknown) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect((err as ApiError).code).toBe('NOT_FOUND');
    expect((err as ApiError).correlationId).toBe('test-correlation-id');
  });

  it('turns a connection failure into NetworkError', async () => {
    server.use(mswHttp.get('/api/down', () => HttpResponse.error()));
    await expect(http.get('/down')).rejects.toBeInstanceOf(NetworkError);
  });

  it('ends the session on 401 SESSION_TIMED_OUT', async () => {
    useSessionStore.setState({ status: 'authenticated' });
    server.use(mswHttp.get('/api/secure', () => problem(401, 'SESSION_TIMED_OUT', 'Idle.')));
    await expect(http.get('/secure')).rejects.toBeInstanceOf(ApiError);
    expect(sessionStore.get().status).toBe('anonymous');
    expect(sessionStore.get().endReason).toBe('timed_out');
  });

  it('does not end the session for a silent 401 (boot-time /me)', async () => {
    useSessionStore.setState({ status: 'authenticated' });
    server.use(mswHttp.get('/api/secure', () => problem(401, 'UNAUTHENTICATED', 'No.')));
    await expect(http.get('/secure', { meta: { silentUnauthorized: true } })).rejects.toBeInstanceOf(ApiError);
    expect(sessionStore.get().status).toBe('authenticated');
  });

  it('parks a SESSION_EXPIRED request, then retries it after re-authentication', async () => {
    let calls = 0;
    server.use(
      mswHttp.get('/api/secure', () => {
        calls += 1;
        return calls === 1 ? problem(401, 'SESSION_EXPIRED', 'Hard limit.') : HttpResponse.json({ ok: true });
      }),
    );

    const pending = http.get<{ ok: boolean }>('/secure');
    await vi.waitFor(() => expect(useReauthStore.getState().isOpen).toBe(true));

    reauthStore.resolve();
    const response = await pending;
    expect(response.data.ok).toBe(true);
    expect(calls).toBe(2);
    expect(useReauthStore.getState().isOpen).toBe(false);
  });

  it('fails the parked request when re-authentication is cancelled', async () => {
    server.use(mswHttp.get('/api/secure', () => problem(401, 'SESSION_EXPIRED', 'Hard limit.')));
    const pending = http.get('/secure');
    await vi.waitFor(() => expect(useReauthStore.getState().isOpen).toBe(true));
    reauthStore.reject(new Error('cancelled'));
    await expect(pending).rejects.toThrow('cancelled');
  });
});
