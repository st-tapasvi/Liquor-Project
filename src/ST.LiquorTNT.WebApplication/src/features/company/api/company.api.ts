import {
  type CompanyResponse,
  type ExciseResponse,
  http,
  type LiquorCategoryResponse,
  type PagedResult,
  type SupplierCodeListParams,
  type SupplierCodeResponse,
} from '@/core/api';

export const companyApi = {
  companies: async (): Promise<CompanyResponse[]> => {
    const { data } = await http.get<CompanyResponse[]>('/companies');
    return data;
  },
  supplierCodes: async (params: SupplierCodeListParams): Promise<PagedResult<SupplierCodeResponse>> => {
    const { data } = await http.get<PagedResult<SupplierCodeResponse>>('/suppliercodes', { params });
    return data;
  },
  liquorCategories: async (): Promise<LiquorCategoryResponse[]> => {
    const { data } = await http.get<LiquorCategoryResponse[]>('/liquorcategories');
    return data;
  },
  excises: async (): Promise<ExciseResponse[]> => {
    const { data } = await http.get<ExciseResponse[]>('/excises');
    return data;
  },
};
