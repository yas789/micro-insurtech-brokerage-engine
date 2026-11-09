import { test, expect } from '@playwright/test';
import { randomUUID } from 'node:crypto';

async function generate(page) {
  await page.goto('/');
  await page.getByLabel('First name').fill('Jane');
  await page.getByLabel('Last name').fill('Broker');
  await page.getByLabel('Email', { exact: true }).fill(`history-${randomUUID()}@example.com`);
  await page.getByLabel('Postcode').fill('SW1A 1AA');
  await page.getByLabel('Year built').fill('1910');
  await page.getByLabel('Rebuild cost').fill('750000');
  const response = page.waitForResponse((item) => item.url().endsWith('/api/quotes') && item.request().method() === 'POST');
  await page.getByRole('button', { name: 'Generate quotes' }).click();
  const created = await (await response).json();
  await expect(page.locator('#comparison-details')).toContainText(created.quoteReference);
  return created;
}

test('reloads, reopens by keyboard, and removes only browser history', async ({ page }) => {
  const methods = [];
  page.on('request', (item) => { if (item.url().includes('/api/')) methods.push(item.method()); });
  const created = await generate(page);
  await expect(page.locator('#results-title')).toBeFocused();
  await page.reload();
  await expect(page.locator('#comparison-history a')).toHaveCount(1);
  await expect(page.locator('.quote-card')).toHaveCount(0);
  const reopen = page.locator('#comparison-history a');
  await reopen.focus();
  await page.keyboard.press('Enter');
  await expect(page.locator('#form-status')).toContainText('Saved comparison loaded');
  await expect(page.locator('#results-title')).toBeFocused();
  await expect(page.locator('.premium')).toHaveText(['£156.00', '£180.00', '£210.00']);
  expect(methods).toEqual(['POST', 'GET']);
  await page.getByRole('button', { name: `Remove comparison ${created.quoteReference} from this browser` }).click();
  await expect(page.locator('#comparison-history')).toContainText('No recent comparisons');
  await expect(page.locator('#comparison-history')).toBeFocused();
  expect(methods).toEqual(['POST', 'GET']);
  await page.reload();
  await expect(page.locator('#form-status')).toContainText('Saved comparison loaded');
  await expect(page.locator('#comparison-history a')).toHaveCount(1);
  await page.getByRole('button', { name: 'Clear browser history' }).click();
  await expect(page.locator('#history-status')).toContainText('Saved comparisons and private links still exist');
  expect(methods).toEqual(['POST', 'GET', 'GET']);
  await page.reload();
  await expect(page.locator('.quote-card')).toHaveCount(3);
  expect(methods).toEqual(['POST', 'GET', 'GET', 'GET']);
});

test('a private link works in a fresh mobile browser without client identity', async ({ page, browser, baseURL }) => {
  const created = await generate(page);
  const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
  try {
    const fresh = await context.newPage();
    const api = [];
    const errors = [];
    fresh.on('pageerror', (error) => errors.push(error.message));
    fresh.on('request', (item) => { if (item.url().includes('/api/')) api.push(item.method()); });
    await fresh.goto(`${baseURL}/#quote=${created.quoteReference}`);
    await expect(fresh.locator('#form-status')).toContainText('Saved comparison loaded');
    await expect(fresh.locator('#saved-property-summary')).toContainText('SW1A 1AA');
    await expect(fresh.locator('.premium')).toHaveText(['£156.00', '£180.00', '£210.00']);
    await expect(fresh.getByLabel('First name')).toHaveValue('');
    await expect(fresh.getByLabel('Email', { exact: true })).toHaveValue('');
    expect(api).toEqual(['GET']);
    expect(errors).toEqual([]);
    const history = await fresh.evaluate(() => JSON.parse(localStorage.getItem('micro-insurtech.comparisons.v1')));
    expect(history.entries).toEqual([{ quoteReference: created.quoteReference, createdAt: new Date(created.createdAt).toISOString() }]);
    expect(await fresh.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  } finally { await context.close(); }
});

test('invalid, missing, and unavailable private links leave generation usable', async ({ page, baseURL }) => {
  await page.goto(`/#quote=${randomUUID()}`);
  await expect(page.locator('#form-status')).toHaveText('Saved comparison was not found.');
  const api = [];
  page.on('request', (item) => { if (item.url().includes('/api/')) api.push(item.method()); });
  await page.goto(`${baseURL}/#quote=invalid`);
  await expect(page.locator('#form-status')).toHaveText('Comparison link is invalid.');
  expect(api).toEqual([]);
  await expect(page.getByRole('button', { name: 'Generate quotes' })).toBeEnabled();
  const unavailable = process.env.UNAVAILABLE_FRONTEND_BASE_URL || 'http://127.0.0.1:3001';
  await page.goto(`${unavailable}/#quote=${randomUUID()}`);
  await expect(page.locator('#form-status')).toHaveText('Core engine is unavailable.');
  await expect(page.locator('#quote-results')).toHaveAttribute('aria-busy', 'false');
  await expect(page.locator('#comparison-history a')).toHaveCount(0);
});

test('blocked local storage does not prevent creation or private-link retrieval', async ({ page }) => {
  const errors = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.addInitScript(() => Object.defineProperty(window, 'localStorage', { get() { throw new Error('Storage blocked'); } }));
  const created = await generate(page);
  await page.getByRole('link', { name: 'Open private comparison link' }).click();
  await expect(page.locator('#form-status')).toContainText('Saved comparison loaded');
  await expect(page.locator('#comparison-details')).toContainText(created.quoteReference);
  expect(errors).toEqual([]);
});
