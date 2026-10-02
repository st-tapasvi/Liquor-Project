import { zodResolver } from '@hookform/resolvers/zod';
import type { ReactNode } from 'react';
import { FormProvider, useForm } from 'react-hook-form';

import type { FieldConfig } from '@/core/api';
import { defaultValuesFromFields, schemaFromFields } from '@/core/screen-config';

import { renderWithProviders, screen } from '@/test/render';

import { DynamicFormFields } from './DynamicFormFields';

function field(overrides: Partial<FieldConfig> & Pick<FieldConfig, 'name' | 'label'>): FieldConfig {
  return {
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

/** Mirrors how a real configured screen wires the config into react-hook-form. */
function ConfiguredForm({ fields }: { fields: FieldConfig[] }): ReactNode {
  const visible = fields.filter((f) => f.visible).sort((a, b) => a.sequence - b.sequence);
  const form = useForm({
    resolver: zodResolver(schemaFromFields(visible)),
    defaultValues: defaultValuesFromFields(visible),
  });

  return (
    <FormProvider {...form}>
      <DynamicFormFields fields={visible} />
    </FormProvider>
  );
}

const UP_BRAND: FieldConfig[] = [
  field({ name: 'brandName', label: 'Brand name', required: true, sequence: 1 }),
  field({ name: 'gtin', label: 'GTIN', required: true, sequence: 2, maxLength: 14 }),
  field({ name: 'brandCode', label: 'Brand code', visible: false, sequence: 3 }),
];

const MP_BRAND: FieldConfig[] = [
  field({ name: 'brandName', label: 'Brand name', required: true, sequence: 1 }),
  field({ name: 'gtin', label: 'GTIN', visible: false, sequence: 2 }),
  field({ name: 'brandCode', label: 'Brand code', required: true, sequence: 3 }),
  field({ name: 'packingCode', label: 'Packing code', required: true, sequence: 4 }),
];

describe('DynamicFormFields', () => {
  it('renders the fields one excise configures', () => {
    renderWithProviders(<ConfiguredForm fields={UP_BRAND} />);

    expect(screen.getByLabelText(/gtin/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/brand code/i)).not.toBeInTheDocument();
  });

  it('renders different fields for another excise from the same component', () => {
    renderWithProviders(<ConfiguredForm fields={MP_BRAND} />);

    // Same screen, no branching: only the configuration differs (§8.1).
    expect(screen.queryByLabelText(/gtin/i)).not.toBeInTheDocument();
    expect(screen.getByLabelText(/brand code/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/packing code/i)).toBeInTheDocument();
  });

  it('disables a field the configuration marks read-only', () => {
    renderWithProviders(<ConfiguredForm fields={[field({ name: 'code', label: 'Code', readOnly: true })]} />);

    expect(screen.getByLabelText(/code/i)).toBeDisabled();
  });

  it('renders the configured options of a select field', () => {
    renderWithProviders(
      <ConfiguredForm
        fields={[
          field({
            name: 'liquorType',
            label: 'Liquor type',
            kind: 'select',
            required: true,
            options: [
              { value: 'IMFL', label: 'IMFL' },
              { value: 'BEER', label: 'Beer' },
            ],
          }),
        ]}
      />,
    );

    expect(screen.getByLabelText(/liquor type/i)).toBeInTheDocument();
  });

  it('shows the helper text the backend supplied', () => {
    renderWithProviders(
      <ConfiguredForm
        fields={[field({ name: 'gtin', label: 'GTIN', helperText: 'Fourteen digits from the portal.' })]}
      />,
    );

    expect(screen.getByText('Fourteen digits from the portal.')).toBeInTheDocument();
  });
});
