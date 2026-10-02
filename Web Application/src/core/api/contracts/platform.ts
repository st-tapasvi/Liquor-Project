import type { IstDateTime } from './common';

/**
 * Platform configuration the frontend reads at start-up. These three endpoints are what make one
 * application serve eleven excises without excise-specific code (specification §8).
 *
 * They are not implemented by the API yet; docs/backend-changes.md specifies them. Until they exist the
 * MSW handlers answer them, and the app degrades safely: no flags means every flagged module is off.
 */

/** GET /api/app/modules — which flagged modules this installation has (§8.2 feature flags). */
export interface ModuleFlagsResponse {
  /** Keys of the modules that are switched on. A key that is absent is off. */
  enabled: string[];
}

/** One company the signed-in user may work in. */
export interface CompanyOption {
  companyId: number;
  companyName: string;
  /** Excise this company files to (UP, RJ, MP, …). Drives screen configuration, never frontend branching. */
  exciseCode: string;
}

/** One plant within a company. */
export interface PlantOption {
  plantId: number;
  plantName: string;
  companyId: number;
}

/** GET /api/app/tenant — the companies and plants this user may select (§9 tenant isolation). */
export interface TenantContextResponse {
  companies: CompanyOption[];
  plants: PlantOption[];
  /** What the API will assume if the user does not choose. Null when the user must pick. */
  defaultCompanyId: number | null;
  defaultPlantId: number | null;
}

/** How a configured field is rendered. Deliberately small: the backend describes intent, not layout. */
export type FieldKind = 'text' | 'number' | 'date' | 'select' | 'checkbox' | 'textarea';

/**
 * One field of a configured screen. This is the mechanism that replaces `if (excise === "UP")`:
 * whether the brand identifier is GTIN, ETIN or brand code + packing code is a difference in this
 * data, not in the code that renders it (§8.1).
 */
export interface FieldConfig {
  /** Matches the form value key and the API payload property. */
  name: string;
  label: string;
  kind: FieldKind;
  visible: boolean;
  required: boolean;
  readOnly: boolean;
  /** Order within the screen; ascending. */
  sequence: number;
  maxLength: number | null;
  helperText: string | null;
  /** Options for `kind: 'select'`; empty for every other kind. */
  options: { value: string; label: string }[];
}

/** GET /api/app/screen-config/{screenKey} — the field definitions for one screen, for this excise. */
export interface ScreenConfigResponse {
  screenKey: string;
  exciseCode: string;
  fields: FieldConfig[];
  /** Server time the configuration was last changed; used to bust the cache. */
  updatedAt: IstDateTime | null;
}
