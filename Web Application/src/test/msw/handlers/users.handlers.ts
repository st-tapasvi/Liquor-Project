import { http, HttpResponse } from 'msw';

import type { CreateUserRequest, PagedResult, UpdateUserRequest, UserResponse } from '@/core/api';

import { makeUser } from '../../factories/user.factory';
import { problem } from '../problem';

export const mockUsers: UserResponse[] = [
  makeUser({ id: 1, userName: 'admin', fullName: 'Administrator', roleId: 1 }),
  makeUser({ id: 7, userName: 'ravi.k', fullName: 'Ravi Kumar', email: 'ravi@example.com', roleId: 2, companyId: 5 }),
  makeUser({
    id: 8,
    userName: 'locked.user',
    fullName: 'Locked User',
    lockedUntil: '2026-09-29T10:07:00' as never,
    failedLoginAttempts: 3,
  }),
  makeUser({ id: 9, userName: 'inactive.user', fullName: 'Inactive User', isActive: false }),
];

function find(id: string | readonly string[] | undefined): UserResponse | undefined {
  return mockUsers.find((u) => u.id === Number(id));
}

export const usersHandlers = [
  http.get('/api/users', ({ request }) => {
    const url = new URL(request.url);
    const search = (url.searchParams.get('search') ?? '').toLowerCase();
    const page = Number(url.searchParams.get('page') ?? '1');
    const pageSize = Number(url.searchParams.get('pageSize') ?? '50');
    const filtered = mockUsers.filter(
      (u) => u.userName.toLowerCase().includes(search) || (u.fullName ?? '').toLowerCase().includes(search),
    );
    const result: PagedResult<UserResponse> = {
      items: filtered.slice((page - 1) * pageSize, page * pageSize),
      page,
      pageSize,
      totalCount: filtered.length,
    };
    return HttpResponse.json(result);
  }),

  http.get('/api/users/:id', ({ params }) => {
    const user = find(params['id']);
    return user ? HttpResponse.json(user) : problem(404, 'NOT_FOUND', 'User not found.');
  }),

  http.post('/api/users', async ({ request }) => {
    const body = (await request.json()) as CreateUserRequest;
    if (mockUsers.some((u) => u.userName === body.userName))
      return problem(409, 'USERNAME_TAKEN', 'User name already in use.');
    if (body.password.length < 12) {
      return problem(400, 'VALIDATION_FAILED', 'Validation failed.', {
        errors: { password: ['Password must be at least 12 characters.'] },
      });
    }
    const user = makeUser({ ...body, isActive: true });
    mockUsers.push(user);
    return HttpResponse.json(user, { status: 201 });
  }),

  http.put('/api/users/:id', async ({ params, request }) => {
    const user = find(params['id']);
    if (!user) return problem(404, 'NOT_FOUND', 'User not found.');
    Object.assign(user, (await request.json()) as UpdateUserRequest);
    return HttpResponse.json(user);
  }),

  http.post('/api/users/:id/activate', ({ params }) => {
    const user = find(params['id']);
    if (!user) return problem(404, 'NOT_FOUND', 'User not found.');
    user.isActive = true;
    return HttpResponse.json(user);
  }),
  http.post('/api/users/:id/deactivate', ({ params }) => {
    const user = find(params['id']);
    if (!user) return problem(404, 'NOT_FOUND', 'User not found.');
    if (user.id === 1) return problem(409, 'CANNOT_DEACTIVATE_SELF', 'You cannot deactivate your own account.');
    user.isActive = false;
    return HttpResponse.json(user);
  }),
  http.post('/api/users/:id/unlock', ({ params }) => {
    const user = find(params['id']);
    if (!user) return problem(404, 'NOT_FOUND', 'User not found.');
    user.lockedUntil = null;
    user.isBlocked = false;
    user.failedLoginAttempts = 0;
    return HttpResponse.json(user);
  }),
];
