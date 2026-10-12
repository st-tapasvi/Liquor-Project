import {
  type CreateUserRequest,
  type ExciseResponse,
  http,
  type PagedResult,
  type PageResponse,
  type PasswordPolicyResponse,
  type ResetUserPasswordRequest,
  type RoleResponse,
  type RoleRightsResponse,
  type CompanyResponse,
  type SaveRoleRequest,
  type SupplierCodeResponse,
  type UpdateUserRequest,
  type UpdateRoleRightsRequest,
  type UpdateUserRolesRequest,
  type UserAccessResponse,
  type UserListParams,
  type UserResponse,
} from '@/core/api';

export const usersApi = {
  list: async (params: UserListParams): Promise<PagedResult<UserResponse>> => {
    const { data } = await http.get<PagedResult<UserResponse>>('/users', { params });
    return data;
  },
  getById: async (id: number): Promise<UserResponse> => {
    const { data } = await http.get<UserResponse>(`/users/${encodeURIComponent(id)}`);
    return data;
  },
  create: async (request: CreateUserRequest): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>('/users', request);
    return data;
  },
  update: async (id: number, request: UpdateUserRequest): Promise<UserResponse> => {
    const { data } = await http.put<UserResponse>(`/users/${encodeURIComponent(id)}`, request);
    return data;
  },
  activate: async (id: number): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>(`/users/${encodeURIComponent(id)}/activate`);
    return data;
  },
  deactivate: async (id: number): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>(`/users/${encodeURIComponent(id)}/deactivate`);
    return data;
  },
  remove: async (id: number): Promise<void> => {
    await http.delete(`/users/${encodeURIComponent(id)}`);
  },
  unlock: async (id: number): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>(`/users/${encodeURIComponent(id)}/unlock`);
    return data;
  },
  resetPassword: async (id: number, request: ResetUserPasswordRequest): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>(`/users/${encodeURIComponent(id)}/resetpassword`, request);
    return data;
  },
  access: async (id: number): Promise<UserAccessResponse> => {
    const { data } = await http.get<UserAccessResponse>(`/users/${encodeURIComponent(id)}/access`);
    return data;
  },
  updateRoles: async (id: number, request: UpdateUserRolesRequest): Promise<UserAccessResponse> => {
    const { data } = await http.put<UserAccessResponse>(`/users/${encodeURIComponent(id)}/roles`, request);
    return data;
  },
};

export const rolesApi = {
  list: async (): Promise<RoleResponse[]> => {
    const { data } = await http.get<RoleResponse[]>('/roles');
    return data;
  },
  get: async (id: number): Promise<RoleResponse> => {
    const { data } = await http.get<RoleResponse>(`/roles/${encodeURIComponent(id)}`);
    return data;
  },
  create: async (request: SaveRoleRequest): Promise<RoleResponse> => {
    const { data } = await http.post<RoleResponse>('/roles', request);
    return data;
  },
  update: async (id: number, request: SaveRoleRequest): Promise<RoleResponse> => {
    const { data } = await http.put<RoleResponse>(`/roles/${encodeURIComponent(id)}`, request);
    return data;
  },
  remove: async (id: number): Promise<void> => {
    await http.delete(`/roles/${encodeURIComponent(id)}`);
  },
  rights: async (id: number): Promise<RoleRightsResponse> => {
    const { data } = await http.get<RoleRightsResponse>(`/roles/${encodeURIComponent(id)}/rights`);
    return data;
  },
  updateRights: async (id: number, request: UpdateRoleRightsRequest): Promise<void> => {
    await http.put(`/roles/${encodeURIComponent(id)}/rights`, request);
  },
  pages: async (): Promise<PageResponse[]> => {
    const { data } = await http.get<PageResponse[]>('/pages');
    return data;
  },
};

export const userLookupsApi = {
  companies: async (): Promise<CompanyResponse[]> => {
    const { data } = await http.get<CompanyResponse[]>('/companies');
    return data;
  },
  supplierCodes: async (): Promise<SupplierCodeResponse[]> => {
    const { data } = await http.get<PagedResult<SupplierCodeResponse>>('/suppliercodes', {
      params: { page: 1, pageSize: 200 },
    });
    return data.items;
  },
  excises: async (): Promise<ExciseResponse[]> => {
    const { data } = await http.get<ExciseResponse[]>('/excises');
    return data;
  },
  passwordPolicies: async (): Promise<PasswordPolicyResponse[]> => {
    const { data } = await http.get<PasswordPolicyResponse[]>('/passwordpolicies');
    return data;
  },
};
