export interface MessageResponse {
  message: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export type IstDateTime = string & { readonly __brand: 'IstDateTime' };
