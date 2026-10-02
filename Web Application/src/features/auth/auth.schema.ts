import { z } from 'zod';

/**
 * Client-side validation is a usability aid only. The API is authoritative: it applies the role's
 * password policy and returns 400 VALIDATION_FAILED with per-field messages, which the forms show.
 * Limits here mirror the API's validators so obviously wrong input never leaves the browser.
 */
export const userNameSchema = z.string().trim().min(1, 'User name is required.').max(50, 'User name is too long.');

export const passwordSchema = z.string().min(1, 'Password is required.').max(128, 'Password is too long.');

export const loginSchema = z.object({
  userName: userNameSchema,
  password: passwordSchema,
});
export type LoginFormValues = z.infer<typeof loginSchema>;

export const changePasswordSchema = z
  .object({
    userName: userNameSchema,
    currentPassword: passwordSchema,
    newPassword: z.string().min(6, 'New password must be at least 6 characters.').max(128, 'Password is too long.'),
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, { path: ['confirmPassword'], message: 'Passwords do not match.' })
  .refine((v) => v.newPassword !== v.currentPassword, {
    path: ['newPassword'],
    message: 'New password must differ from the current one.',
  });
export type ChangePasswordFormValues = z.infer<typeof changePasswordSchema>;

export const forgotStartSchema = z.object({ userName: userNameSchema });
export type ForgotStartFormValues = z.infer<typeof forgotStartSchema>;

export const forgotVerifySchema = z.object({ answer: z.string().trim().min(1, 'Answer is required.').max(200) });
export type ForgotVerifyFormValues = z.infer<typeof forgotVerifySchema>;

export const forgotResetSchema = z
  .object({
    newPassword: z.string().min(6, 'New password must be at least 6 characters.').max(128),
    confirmPassword: z.string(),
  })
  .refine((v) => v.newPassword === v.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match.',
  });
export type ForgotResetFormValues = z.infer<typeof forgotResetSchema>;

export const reauthSchema = z.object({ password: passwordSchema });
export type ReauthFormValues = z.infer<typeof reauthSchema>;
