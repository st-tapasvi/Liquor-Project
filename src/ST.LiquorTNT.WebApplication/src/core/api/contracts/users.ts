import type { IstDateTime } from './common';

/** GET /api/users/{id} and every user-returning action. The password is never part of it. */
export interface UserResponse {
  id: number;
  userName: string;
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
  roleId: number;
  companyId: number | null;
  isActive: boolean;
  isBlocked: boolean;
  failedLoginAttempts: number;
  lockedUntil: IstDateTime | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
  lastLoginAt: IstDateTime | null;
  createdAt: IstDateTime;
}

/** GET /api/users?search=&page=&pageSize= */
export interface UserListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

/** POST /api/users */
export interface CreateUserRequest {
  userName: string;
  password: string;
  roleId: number;
  companyId: number | null;
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
  forcePasswordChange: boolean;
}

/** PUT /api/users/{id} – send ALL fields; an omitted field is cleared by the API. */
export interface UpdateUserRequest {
  roleId: number;
  companyId: number | null;
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
}
