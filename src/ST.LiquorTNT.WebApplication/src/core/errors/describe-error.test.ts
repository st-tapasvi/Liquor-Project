import { describeError, isChunkLoadError } from './app-error';

describe('describeError', () => {
  it('never shows a raw script error, file name or URL', () => {
    const chunk = new TypeError(
      'Failed to fetch dynamically imported module: http://localhost:5173/src/features/dashboard/DashboardPage.tsx?t=1',
    );
    expect(isChunkLoadError(chunk)).toBe(true);
    expect(describeError(chunk)).toBe(
      'This screen could not be loaded. Please reload the page to get the latest version.',
    );
    expect(describeError(new Error('Cannot read properties of undefined (reading "x")'))).toBe(
      'Something went wrong. Please try again.',
    );
  });
});
