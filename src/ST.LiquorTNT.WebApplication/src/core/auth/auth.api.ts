import type {
  CurrentUserResponse,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  MyPermissionsResponse,
  SupplierCodeResponse,
} from '../api/contracts';
import { http } from '../api/http';

import type { SessionAccess } from './session.store';

export const authApi = {

  login: async (request: LoginRequest, options?: { forReauth?: boolean }): Promise<LoginResponse> => {
    const { data } = await http.post<LoginResponse>('/auth/login', request, {
      meta: options?.forReauth ? { skipReauth: true, silentUnauthorized: true } : { silentUnauthorized: true },
    });
    return data;
  },

  /** Ends the cookie's session on the server, which also expires both cookies in the browser. */
  logout: async (): Promise<MessageResponse> => {
    const { data } = await http.post<MessageResponse>('/auth/logout', undefined, {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  me: async (): Promise<CurrentUserResponse> => {
    const { data } = await http.get<CurrentUserResponse>('/auth/me', {
      meta: { silentUnauthorized: true, skipReauth: true },
    });
    return data;
  },

  myPermissions: async (): Promise<MyPermissionsResponse> => {
    const { data } = await http.get<MyPermissionsResponse>('/auth/mypermissions', {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  mySupplierCodes: async (): Promise<SupplierCodeResponse[]> => {
    const { data } = await http.get<SupplierCodeResponse[]>('/auth/mysuppliercodes', {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  selectSupplierCode: async (supplierCodeId: number): Promise<MyPermissionsResponse> => {
    const { data } = await http.post<MyPermissionsResponse>('/auth/selectsuppliercode', { supplierCodeId });
    return data;
  },
};

export async function loadAccess(): Promise<SessionAccess> {
  const [rights, supplierCodes] = await Promise.all([authApi.myPermissions(), authApi.mySupplierCodes()]);
  return {
    isSuperAdmin: rights.isSuperAdmin,
    granted: rights.permissions,
    activeSupplierCode: rights.activeSupplierCode,
    supplierCodes,
  };
}

export async function selectSupplierCode(
  supplierCodeId: number,
  supplierCodes: readonly SupplierCodeResponse[],
): Promise<SessionAccess> {
  const rights = await authApi.selectSupplierCode(supplierCodeId);
  return {
    isSuperAdmin: rights.isSuperAdmin,
    granted: rights.permissions,
    activeSupplierCode: rights.activeSupplierCode,
    supplierCodes,
  };
}
