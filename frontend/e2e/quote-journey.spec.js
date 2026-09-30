import { test, expect } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const verifier = fileURLToPath(new URL('../../scripts/verify-quote-journey.py', import.meta.url));

async function fillQuote(page, postcode = 'SW1A 1AA') {
  const email = `browser-${randomUUID()}@example.com`;
  await page.getByLabel('First name').fill('Jane');
  await page.getByLabel('Last name').fill('Broker');
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Postcode').fill(postcode);
  await page.getByLabel('Year built').fill('1910');
  await page.getByLabel('Rebuild cost').fill('750000');
  return email;
}

for (const [postcode, region] of [['SW1A 1AA', 'London'], ['ZZ99 9ZZ', null]]) {
  test(`quotes persist through the browser for ${postcode}`, async ({ page, baseURL }) => {
    const apiRequests = [];
    const pageErrors = [];
    page.on('request', (request) => {
      if (request.url().includes('/api/')) apiRequests.push(request.url());
    });
    page.on('pageerror', (error) => pageErrors.push(error.message));
    await page.goto('/');
    const email = await fillQuote(page, postcode);
    const responsePromise = page.waitForResponse((response) => response.url().endsWith('/api/quotes'));
    await page.getByRole('button', { name: 'Generate quotes' }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(200);
    const { quotes } = await response.json();

    await expect(page.getByRole('status')).toHaveText('Quotes returned in premium order.');
    await expect(page.locator('.quote-card h3')).toHaveText(['NichePropertyCover', 'AXAScheme', 'AvivaScheme']);
    await expect(page.locator('.premium')).toHaveText(['£156.00', '£180.00', '£210.00']);
    await expect(page.locator('.quote-card')).toHaveCount(3);
    for (const card of await page.locator('.quote-card').all()) {
      await expect(card).toContainText(region || 'Unavailable');
    }
    expect(quotes.every((quote) => quote.region === region)).toBe(true);
    expect(apiRequests).toEqual([`${baseURL}/api/quotes`]);
    expect(pageErrors).toEqual([]);

    // Verify these exact browser results against the real SQL Server database.
    execFileSync('python3', [verifier, '--verify-saved-response'], {
      input: JSON.stringify({ email, postcode, quotes }),
      stdio: ['pipe', 'pipe', 'pipe'],
      timeout: 30_000,
    });
  });
}

test('shows core validation errors and restores the submit button', async ({ page }) => {
  await page.goto('/');
  await fillQuote(page);
  await page.getByLabel('Year built').fill('1499');
  // Exercise server-side validation independently of native browser constraints.
  await page.locator('#quote-form').evaluate((form) => { form.noValidate = true; });
  await page.getByRole('button', { name: 'Generate quotes' }).click();
  await expect(page.getByRole('status')).toHaveText('Property year built must be between 1500 and 2100.');
  await expect(page.locator('.quote-card')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Generate quotes' })).toBeEnabled();
});

test('shows a safe error when the core API is unavailable', async ({ page }) => {
  await page.goto(process.env.UNAVAILABLE_FRONTEND_BASE_URL || 'http://127.0.0.1:3001');
  await fillQuote(page);
  await page.getByRole('button', { name: 'Generate quotes' }).click();
  await expect(page.getByRole('status')).toHaveText('Core engine is unavailable.');
  await expect(page.locator('.quote-card')).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Generate quotes' })).toBeEnabled();
});
