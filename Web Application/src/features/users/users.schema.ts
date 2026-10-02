import { z } from 'zod';

import type { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/core/api';

import { emptyToNull, nullToEmpty } from '@/shared/utils';

const optionalText = (max: number) => z.string().trim().max(max, `At most ${max} characters.`);

const baseFields = {
  fullName: optionalText(100),
  email: optionalText(100).refine((v) => v === '' || z.email().safeParse(v).success, 'Enter a valid e-mail address.'),
  phone: optionalText(20),
  employeeCode: optionalText(30),
  roleId: z.number({ error: 'Role is required.' }).int().positive('Role is required.'),
  companyId: z.number().int().positive().nullable(),
};

export const createUserSchema = z.object({
  ...baseFields,
  userName: z
    .string()
    .trim()
    .min(1, 'User name is required.')
    .max(50, 'At most 50 characters.')
    .regex(/^[A-Za-z0-9._@-]+$/, 'Only letters, digits and . _ @ - are allowed.'),
  password: z.string().min(6, 'At least 6 characters.').max(128),
  forcePasswordChange: z.boolean(),
});
export type CreateUserFormValues = z.infer<typeof createUserSchema>;

export const updateUserSchema = z.object(baseFields);
export type UpdateUserFormValues = z.infer<typeof updateUserSchema>;

export function toCreateRequest(values: CreateUserFormValues): CreateUserRequest {
  return {
    userName: values.userName,
    password: values.password,
    roleId: values.roleId,
    companyId: values.companyId,
    fullName: emptyToNull(values.fullName),
    email: emptyToNull(values.email),
    phone: emptyToNull(values.phone),
    employeeCode: emptyToNull(values.employeeCode),
    forcePasswordChange: values.forcePasswordChange,
  };
}

export function toUpdateRequest(values: UpdateUserFormValues): UpdateUserRequest {
  return {
    roleId: values.roleId,
    companyId: values.companyId,
    fullName: emptyToNull(values.fullName),
    email: emptyToNull(values.email),
    phone: emptyToNull(values.phone),
    employeeCode: emptyToNull(values.employeeCode),
  };
}

export function fromUser(user: UserResponse): UpdateUserFormValues {
  return {
    roleId: user.roleId,
    companyId: user.companyId,
    fullName: nullToEmpty(user.fullName),
    email: nullToEmpty(user.email),
    phone: nullToEmpty(user.phone),
    employeeCode: nullToEmpty(user.employeeCode),
  };
}
