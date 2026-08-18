/**
 * Hero stats below the fold.
 * Slot 2 (streamed line count) stays hidden until showStreamCounter is true
 * and a real API exists. Do not invent an ingest-count endpoint here.
 *
 * Wanted API shape (not implemented):
 *   GET https://api.qiklog.com/v1/stats/streamed
 *   200 { "total": 123456 }
 * Public, no auth, lifetime ingested lines. Fail closed (render nothing).
 */
export const heroStats = {
  median: '278ms',
  medianLabel: 'median, terminal to browser, measured',
  showStreamCounter: false,
  streamCountUrl: '/api/stats/streamed',
};
