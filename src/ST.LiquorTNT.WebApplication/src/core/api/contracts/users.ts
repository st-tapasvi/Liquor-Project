import type { IstDateTime } from './common';

/** GET /api/users/{id} and every user-returning action. The password is never part of it. */
export interface UserResponse {
  id: number;
  userName: string;
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
  companyId: number | null;
  isActive: boolean;
  isBlocked: boolean;
  failedLoginAttempts: number;
  lockedUntil: IstDateTime | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
  lastLoginAt: IstDateTime | null;
  createdAt: IstDateTime | null;
}

/** GET /api/users?search=&page=&pageSize= */
export interface UserListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

/** A role given to a user for one supplier code, or for every supplier code of the company when null. */
export interface UserRoleAssignment {
  roleId: number;
  supplierCodeId: number | null;
}

/** A custom right given to a user for one supplier code, or for every supplier code when null. */
export interface UserRightAssignment {
  pageActionId: number;
  supplierCodeId: number | null;
}

/** POST /api/users */
export interface CreateUserRequest {
  userName: string;
  password: string;
  companyId: number | null;
  roles: UserRoleAssignment[];
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
  forcePasswordChange: boolean;
}

/** PUT /api/users/{id} – profile fields only. Roles and rights change through /roles and /rights. */
export interface UpdateUserRequest {
  fullName: string | null;
  email: string | null;
  phone: string | null;
  employeeCode: string | null;
}

export interface UserRoleResponse {
  roleId: number;
  roleName: string;
  supplierCodeId: number | null;
  /** "RJ CL 550", or "All supplier codes" when the role covers the whole company. */
  supplierCodeName: string;
}

export interface UserRightResponse {
  pageActionId: number;
  permissionKey: string;
  actionName: string;
  supplierCodeId: number | null;
  supplierCodeName: string;
}

/** GET /api/users/{id}/access */
export interface UserAccessResponse {
  userId: number;
  userName: string;
  roles: UserRoleResponse[];
  rights: UserRightResponse[];
}

/** PUT /api/users/{id}/roles – the FULL list; anything not in it is taken away. */
export interface UpdateUserRolesRequest {
  roles: UserRoleAssignment[];
}

/** PUT /api/users/{id}/rights – the FULL list; anything not in it is taken away. */
export interface UpdateUserRightsRequest {
  rights: UserRightAssignment[];
}
