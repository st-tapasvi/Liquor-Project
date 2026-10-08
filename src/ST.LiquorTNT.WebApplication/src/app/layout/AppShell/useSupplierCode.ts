import { useMutation, useQueryClient } from '@tanstack/react-query';

import { selectSupplierCode, useCurrentUser, useSessionStore } from '@/core/auth';

import { useSnackbar } from '@/shared/hooks';

export function useSwitchSupplierCode() {
  const user = useCurrentUser();
  const setAccess = useSessionStore((s) => s.setAccess);
  const queryClient = useQueryClient();
  const snackbar = useSnackbar();

  return useMutation({
    mutationKey: ['auth', 'select-supplier-code'],
    mutationFn: (supplierCodeId: number) => selectSupplierCode(supplierCodeId, user.supplierCodes),
    onSuccess: (access) => {
      queryClient.clear();
      setAccess(access);
      if (access.activeSupplierCode) snackbar.success(`Working in ${access.activeSupplierCode.displayName}.`);
    },
  });
}
