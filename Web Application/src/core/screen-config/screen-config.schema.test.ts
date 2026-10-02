import type { FieldConfig } from '../api/contracts';

import { defaultValuesFromFields, schemaFromFields } from './screen-config.schema';

function field(overrides: Partial<FieldConfig> & Pick<FieldConfig, 'name'>): FieldConfig {
  return {
    label: overrides.name,
    kind: 'text',
    visible: true,
    required: false,
    readOnly: false,
    sequence: 1,
    maxLength: null,
    helperText: null,
    options: [],
    ...overrides,
  };
}

describe('schema built from screen configuration', () => {
  it('validates only the fields this excise actually has', () => {
    // UP identifies a brand by GTIN; MP has no GTIN field at all (§8.1).
    const up = schemaFromFields([
      field({ name: 'gtin', required: true }),
      field({ name: 'brandName', required: true }),
    ]);
    const mp = schemaFromFields([
      field({ name: 'brandCode', required: true }),
      field({ name: 'brandName', required: true }),
    ]);

    expect(up.safeParse({ gtin: '08901234567890', brandName: 'A' }).success).toBe(true);
    expect(mp.safeParse({ brandCode: 'BC-1', brandName: 'A' }).success).toBe(true);
    // The same payload cannot satisfy both, which is the whole point of configuring the screen.
    expect(mp.safeParse({ gtin: '08901234567890', brandName: 'A' }).success).toBe(false);
  });

  it('rejects an empty value for a configured-mandatory field', () => {
    const schema = schemaFromFields([field({ name: 'brandName', required: true, label: 'Brand name' })]);

    const result = schema.safeParse({ brandName: '   ' });
    expect(result.success).toBe(false);
    expect(JSON.stringify(result.error?.issues)).toContain('Brand name is required.');
  });

  it('does not require input for a read-only field, which the user cannot fill', () => {
    const schema = schemaFromFields([field({ name: 'code', required: true, readOnly: true })]);

    expect(schema.safeParse({ code: '' }).success).toBe(true);
  });

  it('enforces the configured maximum length', () => {
    const schema = schemaFromFields([field({ name: 'gtin', maxLength: 14, label: 'GTIN' })]);

    expect(schema.safeParse({ gtin: '1'.repeat(14) }).success).toBe(true);
    expect(schema.safeParse({ gtin: '1'.repeat(15) }).success).toBe(false);
  });

  it('ignores hidden fields entirely', () => {
    const schema = schemaFromFields([field({ name: 'gtin', visible: false, required: true })]);

    expect(schema.safeParse({}).success).toBe(true);
  });

  it('builds controlled default values matching the field kinds', () => {
    const values = defaultValuesFromFields([
      field({ name: 'brandName' }),
      field({ name: 'packSize', kind: 'number' }),
      field({ name: 'isActive', kind: 'checkbox' }),
      field({ name: 'hidden', visible: false }),
    ]);

    expect(values).toEqual({ brandName: '', packSize: null, isActive: false });
  });
});
