import type { IstDateTime } from './common';

export interface SupplierCodeResponse {
  id: number;
  companyId: number;
  companyName: string | null;
  franchiseName: string | null;
  exciseId: number;
  exciseCode: string;
  supplierCode: string;
  liquorCategoryId: number;
  liquorCategoryCode: string;
  displayName: string;
  isActive: boolean;
  createdAt: IstDateTime | null;
}

/** GET /api/suppliercodes?search=&page=&pageSize= */
export interface SupplierCodeListParams {
  search?: string;
  page?: number;
  pageSize?: number;
}

/** POST /api/suppliercodes */
export interface CreateSupplierCodeRequest {
  companyId: number;
  franchiseName: string | null;
  exciseId: number;
  supplierCode: string;
  liquorCategoryId: number;
}

/** PUT /api/suppliercodes/{id} – the company of a supplier code never changes. */
export interface UpdateSupplierCodeRequest {
  franchiseName: string | null;
  exciseId: number;
  supplierCode: string;
  liquorCategoryId: number;
}

/** GET /api/companies (Super Admin) */
export interface CompanyResponse {
  id: number;
  companyName: string | null;
  aliasName: string | null;
  city: string | null;
  isActive: boolean;
  supplierCodeCount: number;
}

/** GET /api/excises */
export interface ExciseResponse {
  id: number;
  exciseCode: string;
  exciseName: string;
  isActive: boolean;
}

/** GET /api/liquorcategories */
export interface LiquorCategoryResponse {
  id: number;
  categoryCode: string;
  categoryName: string;
  description: string | null;
  isActive: boolean;
}

/** POST and PUT /api/liquorcategories */
export interface SaveLiquorCategoryRequest {
  categoryCode: string;
  categoryName: string;
  description: string | null;
}

/** GET /api/roles */
export interface RoleResponse {
  id: number;
  companyId: number | null;
  supplierCodeId: number | null;
  supplierCodeName: string | null;
  roleName: string;
  displayName: string;
  description: string | null;
  isSystem: boolean;
  isTemplate: boolean;
  isAdminRole: boolean;
  isActive: boolean;
  passwordPolicyId: number | null;
}

/** POST and PUT /api/roles */
export interface SaveRoleRequest {
  roleName: string;
  description: string | null;
  isAdminRole: boolean;
  passwordPolicyId: number;
}

/** ANY, ADMIN (needs user.manageadmin to grant) or SYSTEM (never grantable; Super Admin only). */
export type GrantScope = 'ANY' | 'ADMIN' | 'SYSTEM';

export interface PageActionResponse {
  pageActionId: number;
  actionKey: string;
  permissionKey: string;
  actionName: string;
  grantScope: GrantScope;
  granted: boolean;
}

/** GET /api/pages – a page with all its actions (the rows and columns of the rights grid). */
export interface PageResponse {
  pageId: number;
  pageKey: string;
  pageName: string;
  moduleName: string | null;
  actions: PageActionResponse[];
}

/** GET /api/roles/{id}/rights */
export interface RoleRightsResponse {
  roleId: number;
  roleName: string;
  pages: PageResponse[];
}

/** PUT /api/roles/{id}/rights – the FULL list of ticked actions. */
export interface UpdateRoleRightsRequest {
  pageActionIds: number[];
}
