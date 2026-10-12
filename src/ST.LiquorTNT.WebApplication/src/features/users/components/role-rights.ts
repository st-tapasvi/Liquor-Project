import type { ApplicationType, PageActionResponse, PageResponse } from '@/core/api';

export const APPLICATION_TYPES: readonly { value: ApplicationType; label: string }[] = [
  { value: 'WEB', label: 'Web application' },
  { value: 'LINE', label: 'Line application' },
];

export function isGrantable(action: PageActionResponse, canManageAdmins: boolean): boolean {
  if (action.grantScope === 'SYSTEM') return false;
  if (action.grantScope === 'ADMIN') return canManageAdmins;
  return true;
}

function appliesToExcise(exciseCodes: readonly number[] | null | undefined, exciseId: number): boolean {
  return !exciseCodes || exciseCodes.length === 0 || exciseCodes.includes(exciseId);
}

export function pagesForExcise(pages: readonly PageResponse[], exciseId: number | null): PageResponse[] {
  if (exciseId === null) return [];
  return pages
    .filter((p) => appliesToExcise(p.exciseCodes, exciseId))
    .map((p) => ({ ...p, actions: p.actions.filter((a) => appliesToExcise(a.exciseCodes, exciseId)) }))
    .filter((p) => p.actions.length > 0);
}

export function pageSelection(page: PageResponse, selected: ReadonlySet<number>) {
  const count = page.actions.filter((a) => selected.has(a.pageActionId)).length;
  return { count, checked: count > 0, indeterminate: count > 0 && count < page.actions.length };
}

export function togglePage(page: PageResponse, selected: ReadonlySet<number>, canManageAdmins: boolean): Set<number> {
  const next = new Set(selected);
  const grantable = page.actions.filter((a) => isGrantable(a, canManageAdmins));
  const anySelected = page.actions.some((a) => selected.has(a.pageActionId));
  for (const a of grantable) {
    if (anySelected) next.delete(a.pageActionId);
    else next.add(a.pageActionId);
  }
  return next;
}

export function setActions(ids: readonly number[], checked: boolean, selected: ReadonlySet<number>): Set<number> {
  const next = new Set(selected);
  for (const id of ids) {
    if (checked) next.add(id);
    else next.delete(id);
  }
  return next;
}

export function isChanged(id: number, selected: ReadonlySet<number>, baseline: ReadonlySet<number>): boolean {
  return selected.has(id) !== baseline.has(id);
}

export function pageChanged(page: PageResponse, selected: ReadonlySet<number>, baseline: ReadonlySet<number>): boolean {
  return page.actions.some((a) => isChanged(a.pageActionId, selected, baseline));
}

export function changeCount(
  pages: readonly PageResponse[],
  selected: ReadonlySet<number>,
  baseline: ReadonlySet<number>,
): number {
  return pages.reduce(
    (sum, p) => sum + p.actions.filter((a) => isChanged(a.pageActionId, selected, baseline)).length,
    0,
  );
}
