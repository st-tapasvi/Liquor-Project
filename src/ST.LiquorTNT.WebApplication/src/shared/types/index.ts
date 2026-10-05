export interface Option<TValue extends string | number = string | number> {
  value: TValue;
  label: string;
  disabled?: boolean;
}

export type Nullable<T> = T | null;
