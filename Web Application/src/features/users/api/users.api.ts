import {
  type CreateUserRequest,
  http,
  type PagedResult,
  type UpdateUserRequest,
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
};
