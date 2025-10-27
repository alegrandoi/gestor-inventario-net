import { defineConfig } from '@playwright/test';

const fallbackBaseURL = 'http://127.0.0.1:3000';
const rawBaseURL = process.env.PLAYWRIGHT_BASE_URL?.trim();
const resolvedBaseURL =
  rawBaseURL && rawBaseURL.length > 0
    ? rawBaseURL.includes('://')
      ? rawBaseURL
      : `http://${rawBaseURL}`
    : fallbackBaseURL;
const shouldStartLocalServer = resolvedBaseURL === fallbackBaseURL;

const normalizedBase = resolvedBaseURL.replace(/\/$/, '');
if (!process.env.NEXT_PUBLIC_API_BASE_URL) {
  process.env.NEXT_PUBLIC_API_BASE_URL = `${normalizedBase}/api`;
}

export default defineConfig({
  testDir: './specs',
  timeout: 60_000,
  expect: {
    timeout: 5_000
  },
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  use: {
    baseURL: resolvedBaseURL,
    trace: 'on-first-retry'
  },
  webServer: shouldStartLocalServer
    ? {
        command: 'npm run dev -- --hostname 127.0.0.1 --port 3000',
        url: fallbackBaseURL,
        reuseExistingServer: !process.env.CI,
        stdout: 'pipe',
        stderr: 'pipe',
        timeout: 120_000
      }
    : undefined
});
