import { keepPreviousData, useQuery } from '@tanstack/react-query';

import type { SupplierCodeListParams } from '@/core/api';
import { useCompanyScope } from '@/core/auth';

import { companyApi } from './company.api';

export const companyKeys = {
  all: (scope: number) => ['company', scope] as const,
  companies: (scope: number) => [...companyKeys.all(scope), 'companies'] as const,
  supplierCodes: (scope: number, params: SupplierCodeListParams) =>
    [...companyKeys.all(scope), 'supplier-codes', params] as const,
  liquorCategories: (scope: number) => [...companyKeys.all(scope), 'liquor-categories'] as const,
};

export function useCompanies() {
  const scope = useCompanyScope();
  return useQuery({ queryKey: companyKeys.companies(scope), queryFn: companyApi.companies });
}

export function useSupplierCodes(params: SupplierCodeListParams) {
  const scope = useCompanyScope();
  return useQuery({
    queryKey: companyKeys.supplierCodes(scope, params),
    queryFn: () => companyApi.supplierCodes(params),
    placeholderData: keepPreviousData,
  });
}

export function useLiquorCategories() {
  const scope = useCompanyScope();
  return useQuery({ queryKey: companyKeys.liquorCategories(scope), queryFn: companyApi.liquorCategories });
}
