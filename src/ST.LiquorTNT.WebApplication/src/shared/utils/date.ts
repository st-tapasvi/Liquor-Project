import dayjs from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';

dayjs.extend(customParseFormat);

/**
 * API date-times are IST strings without a zone ("2026-09-28T18:30:00"). They are parsed as
 * plain wall-clock values and displayed as-is, never converted to the browser's zone.
 */
const API_FORMATS = [
  'YYYY-MM-DDTHH:mm:ss.SSSSSSS',
  'YYYY-MM-DDTHH:mm:ss.SSS',
  'YYYY-MM-DDTHH:mm:ss',
  'YYYY-MM-DDTHH:mm',
  'YYYY-MM-DD',
];

export const DISPLAY_DATE = 'DD-MM-YYYY';
export const DISPLAY_DATE_TIME = 'DD-MM-YYYY HH:mm';

export function parseApiDate(value: string | null | undefined): dayjs.Dayjs | null {
  if (!value) return null;
  // Strip fractional seconds beyond milliseconds and any accidental zone designator.
  const normalised = value.replace(/(\.\d{3})\d+/, '$1').replace(/(Z|[+-]\d{2}:\d{2})$/, '');
  const parsed = dayjs(normalised, API_FORMATS, true);
  return parsed.isValid() ? parsed : null;
}

export function formatDate(value: string | null | undefined): string {
  const d = parseApiDate(value);
  return d ? d.format(DISPLAY_DATE) : '';
}

export function formatDateTime(value: string | null | undefined): string {
  const d = parseApiDate(value);
  return d ? d.format(DISPLAY_DATE_TIME) : '';
}

/** Minutes from now until the given API time; negative when in the past; null when unparseable. */
export function minutesUntil(value: string | null | undefined, now: dayjs.Dayjs = dayjs()): number | null {
  const d = parseApiDate(value);
  return d ? d.diff(now, 'minute') : null;
}
