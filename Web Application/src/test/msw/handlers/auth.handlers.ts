import { http, HttpResponse } from 'msw';

import type { ActiveSessionSummary, IstDateTime, LoginRequest, LoginResponse, SessionResponse } from '@/core/api';

import { makeCurrentUser } from '../../factories/user.factory';
import { problem } from '../problem';

/**
 * In-memory session for the mock API. Mirrors the cookie contract: login "sets" a session, /me
 * answers 401 until then. (MSW cannot set HttpOnly cookies, so the state is a module variable.)
 */
/** The two devices the user `full` is already signed in on, so the device-limit flow can be exercised. */
function seedOpenSessions(): ActiveSessionSummary[] {
  return [
    {
      id: 71,
      loginAt: '2026-09-28T08:00:00' as IstDateTime,
      lastActivityAt: '2026-09-28T09:30:00' as IstDateTime,
      expiresAt: '2026-09-29T08:00:00' as IstDateTime,
      ipAddress: '192.168.1.20',
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/141.0.0.0 Safari/537.36',
    },
    {
      id: 72,
      loginAt: '2026-09-27T17:45:00' as IstDateTime,
      lastActivityAt: null,
      expiresAt: '2026-09-28T17:45:00' as IstDateTime,
      ipAddress: '192.168.1.55',
      userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) Safari/605.1.15',
    },
  ];
}

/**
 * Name of the mock session cookie. The real API issues an HttpOnly `jwt` cookie that JavaScript cannot
 * read; MSW cannot set an HttpOnly cookie, so the mock uses a separate readable name. What matters for
 * tests is that the session now survives a full page load, which is what makes deep links testable.
 */
export const MOCK_SESSION_COOKIE = 'mock-session';

const SET_SESSION = { 'Set-Cookie': `${MOCK_SESSION_COOKIE}=1; Path=/; SameSite=Lax` };
const CLEAR_SESSION = { 'Set-Cookie': `${MOCK_SESSION_COOKIE}=; Path=/; Max-Age=0; SameSite=Lax` };

export const mockAuthState = {
  loggedIn: false,
  password: 'Admin@123',
  /** Whether the mock API offers "sign out and continue" (the server's SESSION_FULL_BEHAVIOUR). */
  canEndOtherSession: true,
  openSessions: seedOpenSessions(),
  reset() {
    this.loggedIn = false;
    this.canEndOtherSession = true;
    this.openSessions = seedOpenSessions();
  },
};

const ONE_DAY_LATER = '2026-10-01T09:00:00' as IstDateTime;

export const authHandlers = [
  http.post('/api/auth/login', async ({ request }) => {
    const body = (await request.json()) as LoginRequest;
    if (!body.userName || !body.password) {
      return problem(400, 'VALIDATION_FAILED', 'Validation failed.', {
        errors: { password: ['Password is required.'] },
      });
    }
    if (body.userName === 'locked')
      return problem(403, 'USER_LOCKED', 'This account is temporarily locked.', { detail: 'Try again after 10:07.' });
    if (body.userName === 'temp') return problem(403, 'PASSWORD_CHANGE_REQUIRED', 'A new password must be set.');

    // `full` is signed in on two devices already: the API checks the limit only after the password.
    if (body.userName === 'full') {
      if (body.password !== mockAuthState.password) {
        return problem(401, 'INVALID_CREDENTIALS', 'User name or password is incorrect.');
      }

      if (mockAuthState.canEndOtherSession && body.endSessionId !== undefined) {
        mockAuthState.openSessions = mockAuthState.openSessions.filter((s) => s.id !== body.endSessionId);
      }

      if (mockAuthState.openSessions.length >= 2) {
        return problem(409, 'SESSION_LIMIT_REACHED', 'Maximum active sessions reached.', {
          detail: mockAuthState.canEndOtherSession
            ? 'This account is already signed in on 2 device(s). Choose one to sign out, or log out there first.'
            : 'This account already has 2 active session(s). Log out of another device first.',
          extensions: {
            sessions: mockAuthState.openSessions,
            canEndOtherSession: mockAuthState.canEndOtherSession,
          },
        });
      }

      mockAuthState.loggedIn = true;
      const ok: LoginResponse = { expiresAt: ONE_DAY_LATER, idleTimeoutMinutes: 60, user: makeCurrentUser() };
      return HttpResponse.json(ok, { headers: SET_SESSION });
    }

    if (body.userName !== 'admin' || body.password !== mockAuthState.password) {
      return problem(401, 'INVALID_CREDENTIALS', 'User name or password is incorrect.');
    }
    mockAuthState.loggedIn = true;
    const response: LoginResponse = { expiresAt: ONE_DAY_LATER, idleTimeoutMinutes: 60, user: makeCurrentUser() };
    return HttpResponse.json(response, { headers: SET_SESSION });
  }),

  http.get('/api/auth/me', ({ cookies }) => {
    // Either the in-memory flag (component tests) or the cookie (a real browser after a reload).
    const signedIn = mockAuthState.loggedIn || cookies[MOCK_SESSION_COOKIE] === '1';
    if (!signedIn) return problem(401, 'UNAUTHENTICATED', 'Authentication is required.');
    mockAuthState.loggedIn = true;
    return HttpResponse.json(makeCurrentUser());
  }),

  http.post('/api/auth/logout', () => {
    mockAuthState.loggedIn = false;
    return HttpResponse.json({ message: 'Logged out.' }, { headers: CLEAR_SESSION });
  }),

  http.post('/api/auth/changepassword', () =>
    HttpResponse.json({ message: 'Password changed. Log in with the new password.' }),
  ),

  http.get('/api/auth/sessions', () => {
    const sessions: SessionResponse[] = [
      {
        id: 41,
        loginAt: '2026-09-28T09:00:00' as IstDateTime,
        lastActivityAt: '2026-09-28T09:40:00' as IstDateTime,
        expiresAt: ONE_DAY_LATER,
        ipAddress: '192.168.1.20',
        userAgent: 'Mozilla/5.0',
        isCurrent: true,
      },
      {
        id: 42,
        loginAt: '2026-09-27T09:00:00' as IstDateTime,
        lastActivityAt: null,
        expiresAt: ONE_DAY_LATER,
        ipAddress: '192.168.1.21',
        userAgent: 'Mozilla/5.0 (Tablet)',
        isCurrent: false,
      },
    ];
    return HttpResponse.json(sessions);
  }),

  http.delete('/api/auth/sessions/:id', ({ params }) =>
    HttpResponse.json({ message: `Session ${String(params['id'])} has been logged out.` }),
  ),

  http.post('/api/auth/forgotpassword/start', () =>
    HttpResponse.json({
      requestToken: 'mock-token',
      questionText: 'What was the name of your first pet?',
      expiresAt: '2026-09-28T10:15:00',
    }),
  ),
  http.post('/api/auth/forgotpassword/verify', async ({ request }) => {
    const body = (await request.json()) as { answer: string };
    if (body.answer.trim().toLowerCase() !== 'tommy')
      return problem(401, 'SECURITY_ANSWER_INCORRECT', 'Answer is incorrect.');
    return HttpResponse.json({ message: 'Answer verified.' });
  }),
  http.post('/api/auth/forgotpassword/reset', () =>
    HttpResponse.json({ message: 'Password has been reset. Log in with the new password.' }),
  ),
  http.get('/api/securityquestions', () =>
    HttpResponse.json([{ id: 1, questionText: 'What is the name of your first school?' }]),
  ),
];
