import {
  type CreateUserRequest,
  http,
  type PagedResult,
  type RoleResponse,
  type CompanyResponse,
  type SupplierCodeResponse,
  type UpdateUserRequest,
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
  unlock: async (id: number): Promise<UserResponse> => {
    const { data } = await http.post<UserResponse>(`/users/${encodeURIComponent(id)}/unlock`);
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
};
