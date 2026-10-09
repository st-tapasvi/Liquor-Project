import { z } from 'zod';

import type { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/core/api';

import { emptyToNull, nullToEmpty } from '@/shared/utils';

const optionalText = (max: number) => z.string().trim().max(max, `At most ${max} characters.`);

const profileFields = {
  fullName: optionalText(100),
  email: optionalText(100).refine((v) => v === '' || z.email().safeParse(v).success, 'Enter a valid e-mail address.'),
  phone: optionalText(20),
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
