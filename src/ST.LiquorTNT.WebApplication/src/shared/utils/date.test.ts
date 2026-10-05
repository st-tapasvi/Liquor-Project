import dayjs from 'dayjs';

import { formatDate, formatDateTime, minutesUntil, parseApiDate } from './date';

describe('date utils', () => {
  it('parses an IST api string without converting it', () => {
    const d = parseApiDate('2026-09-28T18:30:00');
    expect(d?.hour()).toBe(18);
    expect(d?.minute()).toBe(30);
  });

  it('ignores fractional seconds beyond milliseconds', () => {
    expect(parseApiDate('2026-09-28T18:30:00.1234567')?.isValid()).toBe(true);
  });

  it('formats dates for display', () => {
    expect(formatDate('2026-09-28T18:30:00')).toBe('28-09-2026');
    expect(formatDateTime('2026-09-28T18:30:00')).toBe('28-09-2026 18:30');
  });

  it('returns empty string for null or garbage', () => {
    expect(formatDate(null)).toBe('');
    expect(formatDate('not a date')).toBe('');
  });

  it('computes minutes until a deadline', () => {
    const now = dayjs('2026-09-28T18:00:00');
    expect(minutesUntil('2026-09-28T18:30:00', now)).toBe(30);
    expect(minutesUntil('2026-09-28T17:00:00', now)).toBe(-60);
    expect(minutesUntil(null, now)).toBeNull();
  });
});
