import { z } from 'zod';

import type { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/core/api';

import { emptyToNull, nullToEmpty } from '@/shared/utils';

const optionalText = (max: number) => z.string().trim().max(max, `At most ${max} characters.`);

const profileFields = {
  fullName: optionalText(100),
  email: optionalText(100).refine((v) => v === '' || z.email().safeParse(v).success, 'Enter a valid e-mail address.'),
  phone: z
    .string()
    .trim()
    .refine((v) => v === '' || /^\d{10}$/.test(v), 'Enter a 10-digit mobile number.'),
  employeeCode: optionalText(50),
};

export const createUserSchema = z.object({
  ...profileFields,
  userName: z
    .string()
    .trim()
    .min(1, 'User name is required.')
    .max(50, 'At most 50 characters.')
    .regex(/^[A-Za-z0-9._@-]+$/, 'Only letters, digits and . _ @ - are allowed.'),
  password: z.string().min(6, 'At least 6 characters.').max(128),
  companyId: z.number().int().positive().nullable(),
  roleIds: z.array(z.number().int().positive()).min(1, 'Select at least one role.'),
  forcePasswordChange: z.boolean(),
});
export type CreateUserFormValues = z.infer<typeof createUserSchema>;

export const updateUserSchema = z.object(profileFields);
export type UpdateUserFormValues = z.infer<typeof updateUserSchema>;

export const resetPasswordSchema = z
  .object({
    newPassword: z.string().min(6, 'At least 6 characters.').max(128, 'At most 128 characters.'),
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match.',
  });
export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

export const roleProfileSchema = z.object({
  groupName: z.string().trim().min(1, 'Role profile name is required.').max(100, 'At most 100 characters.'),
  description: optionalText(255),
});
export type RoleProfileFormValues = z.infer<typeof roleProfileSchema>;

export function toCreateRequest(values: CreateUserFormValues): CreateUserRequest {
  return {
    userName: values.userName,
    password: values.password,
    companyId: values.companyId,
    roles: values.roleIds.map((roleId) => ({ roleId })),
    fullName: emptyToNull(values.fullName),
    email: emptyToNull(values.email),
    phone: emptyToNull(values.phone),
    employeeCode: emptyToNull(values.employeeCode),
    forcePasswordChange: values.forcePasswordChange,
  };
}

export function toUpdateRequest(values: UpdateUserFormValues): UpdateUserRequest {
  return {
    fullName: emptyToNull(values.fullName),
    email: emptyToNull(values.email),
    phone: emptyToNull(values.phone),
    employeeCode: emptyToNull(values.employeeCode),
  };
}

export function fromUser(user: UserResponse): UpdateUserFormValues {
  return {
    fullName: nullToEmpty(user.fullName),
    email: nullToEmpty(user.email),
    phone: nullToEmpty(user.phone),
    employeeCode: nullToEmpty(user.employeeCode),
  };
}

const requiredId = (message: string) =>
  z
    .number()
    .int()
    .positive()
    .nullable()
    .refine((v) => v !== null, message);

export const ALL_SUPPLIER_CODES = 'all';

export const createRoleSchema = z.object({
  roleName: z.string().trim().min(1, 'Role name is required.').max(100, 'At most 100 characters.'),
  companyId: requiredId('Company is required.'),
  exciseId: requiredId('Excise is required.'),
  supplierCode: z
    .union([z.number().int().positive(), z.literal(ALL_SUPPLIER_CODES)])
    .nullable()
    .refine((v) => v !== null, 'Supplier code is required.'),
  passwordPolicyId: requiredId('Password policy is required.'),
  description: z.string().trim().max(255, 'At most 255 characters.'),
});
export type CreateRoleFormInput = z.input<typeof createRoleSchema>;
export type CreateRoleFormValues = z.output<typeof createRoleSchema>;

export const editRoleSchema = z.object({
  roleName: z.string().trim().min(1, 'Role name is required.').max(100, 'At most 100 characters.'),
  passwordPolicyId: requiredId('Password policy is required.'),
  description: z.string().trim().max(255, 'At most 255 characters.'),
});
export type EditRoleFormInput = z.input<typeof editRoleSchema>;
export type EditRoleFormValues = z.output<typeof editRoleSchema>;
