import { http, HttpResponse } from 'msw';

import type { IstDateTime, ModuleFlagsResponse, ScreenConfigResponse, TenantContextResponse } from '@/core/api';

import { problem } from '../problem';

/**
 * The three platform endpoints of specification §8, as the API will implement them
 * (docs/backend-changes.md). Tests mutate `mockPlatformState` to model a different installation —
 * that is how a flagged-off module is tested without a backend.
 */
export const mockPlatformState = {
  /** Modules this mock installation has. Dispatch and Outbox are on; Plans, Palette, Code pool are off. */
  enabledModules: ['plant', 'dispatch', 'outbox'] as string[],
  companyId: 1 as number | null,
  reset() {
    this.enabledModules = ['plant', 'dispatch', 'outbox'];
    this.companyId = 1;
  },
};

const TENANT: TenantContextResponse = {
  companies: [
    { companyId: 1, companyName: 'Sundaram Distilleries', exciseCode: 'UP' },
    { companyId: 2, companyName: 'Sundaram Beverages (MP)', exciseCode: 'MP' },
  ],
  plants: [
    { plantId: 11, plantName: 'Unit I — Lucknow', companyId: 1 },
    { plantId: 12, plantName: 'Unit II — Kanpur', companyId: 1 },
    { plantId: 21, plantName: 'Unit I — Indore', companyId: 2 },
  ],
  defaultCompanyId: 1,
  defaultPlantId: 11,
};

/**
 * A brand form as two different excises configure it. UP identifies a brand by GTIN; MP identifies it
 * by brand code plus packing code and has no GTIN field at all. The screen that renders both is the
 * same screen — that is the point of §8.1.
 */
const BRAND_FORM: Record<string, ScreenConfigResponse> = {
  UP: {
    screenKey: 'brands.form',
    exciseCode: 'UP',
    updatedAt: '2026-09-01T10:00:00' as IstDateTime,
    fields: [
      {
        name: 'brandName',
        label: 'Brand name',
        kind: 'text',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 1,
        maxLength: 120,
        helperText: null,
        options: [],
      },
      {
        name: 'gtin',
        label: 'GTIN',
        kind: 'text',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 2,
        maxLength: 14,
        helperText: 'Fourteen digits as issued by the portal.',
        options: [],
      },
      {
        name: 'packSize',
        label: 'Pack size (ml)',
        kind: 'number',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 3,
        maxLength: null,
        helperText: null,
        options: [],
      },
      {
        name: 'liquorType',
        label: 'Liquor type',
        kind: 'select',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 4,
        maxLength: null,
        helperText: null,
        options: [
          { value: 'IMFL', label: 'IMFL' },
          { value: 'BEER', label: 'Beer' },
          { value: 'CL', label: 'Country liquor' },
        ],
      },
      {
        name: 'brandCode',
        label: 'Brand code',
        kind: 'text',
        visible: false,
        required: false,
        readOnly: false,
        sequence: 5,
        maxLength: 20,
        helperText: null,
        options: [],
      },
      {
        name: 'packingCode',
        label: 'Packing code',
        kind: 'text',
        visible: false,
        required: false,
        readOnly: false,
        sequence: 6,
        maxLength: 20,
        helperText: null,
        options: [],
      },
    ],
  },
  MP: {
    screenKey: 'brands.form',
    exciseCode: 'MP',
    updatedAt: '2026-09-01T10:00:00' as IstDateTime,
    fields: [
      {
        name: 'brandName',
        label: 'Brand name',
        kind: 'text',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 1,
        maxLength: 120,
        helperText: null,
        options: [],
      },
      {
        name: 'gtin',
        label: 'GTIN',
        kind: 'text',
        visible: false,
        required: false,
        readOnly: false,
        sequence: 2,
        maxLength: 14,
        helperText: null,
        options: [],
      },
      {
        name: 'brandCode',
        label: 'Brand code',
        kind: 'text',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 3,
        maxLength: 20,
        helperText: null,
        options: [],
      },
      {
        name: 'packingCode',
        label: 'Packing code',
        kind: 'text',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 4,
        maxLength: 20,
        helperText: null,
        options: [],
      },
      {
        name: 'packSize',
        label: 'Pack size (ml)',
        kind: 'number',
        visible: true,
        required: true,
        readOnly: false,
        sequence: 5,
        maxLength: null,
        helperText: null,
        options: [],
      },
    ],
  },
};

export const platformHandlers = [
  http.get('/api/app/modules', () => {
    const response: ModuleFlagsResponse = { enabled: [...mockPlatformState.enabledModules] };
    return HttpResponse.json(response);
  }),

  http.get('/api/app/tenant', () => HttpResponse.json(TENANT)),

  http.get('/api/app/screen-config/:screenKey', ({ params, request }) => {
    const screenKey = String(params['screenKey']);
    // The API reads the excise from the session and the company header, not from the query string.
    const company = request.headers.get('X-Company-Id');
    const excise = company === '2' ? 'MP' : 'UP';

    if (screenKey === 'brands.form') return HttpResponse.json(BRAND_FORM[excise]);

    return problem(404, 'ENDPOINT_NOT_FOUND', 'No configuration exists for this screen.', {
      detail: `The excise ${excise} has no configuration for '${screenKey}'.`,
    });
  }),
];
