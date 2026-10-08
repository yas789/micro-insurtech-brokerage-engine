import { describe, expect, it, vi } from 'vitest';
import {
  bindQuoteForm,
  buildQuotePayload,
  formatCurrency,
  formatError,
  handleQuoteSubmit,
  renderQuotes,
} from '../quote-ui.js';

describe('quote frontend', () => {
  it('builds the quote payload from form fields', () => {
    const form = createForm();
    form.elements.firstName.value = ' Jane ';
    form.elements.lastName.value = ' Broker ';
    form.elements.email.value = ' jane@example.com ';
    form.elements.postcode.value = ' SW1A 1AA ';
    form.elements.yearBuilt.value = '1910';
    form.elements.rebuildCost.value = '750000.50';
    form.elements.isUnoccupied.checked = true;

    expect(buildQuotePayload(new FormData(form))).toEqual({
      client: {
        firstName: 'Jane',
        lastName: 'Broker',
        email: 'jane@example.com',
      },
      property: {
        postcode: 'SW1A 1AA',
        yearBuilt: 1910,
        rebuildCost: 750000.50,
        isUnoccupied: true,
      },
    });
  });

  it('sets unoccupied to false when checkbox is clear', () => {
    const form = createForm();

    expect(buildQuotePayload(new FormData(form)).property.isUnoccupied).toBe(false);
  });

  it('renders returned quote cards', () => {
    const results = document.createElement('div');

    renderQuotes(results, [
      {
        underwriterName: 'NichePropertyCover',
        premiumAmount: 156,
        riskRating: 'Medium',
        region: 'London',
      },
      {
        underwriterName: 'AXAScheme',
        premiumAmount: 180,
        riskRating: 'Low',
        region: null,
      },
    ]);

    expect(results.className).toBe('quote-grid');
    expect(results.querySelectorAll('.quote-card')).toHaveLength(2);
    expect(results.textContent).toContain('NichePropertyCover');
    expect(results.textContent).toContain('£156.00');
    expect(results.textContent).toContain('Unavailable');
    expect(results.textContent).toContain('Accept Cover');
  });

  it('clearly disables deferred cover acceptance on every quote', () => {
    const results = document.createElement('div');
    renderQuotes(results, [
      { underwriterName: 'AvivaScheme', premiumAmount: 210 },
      { underwriterName: 'AXAScheme', premiumAmount: 180 },
    ]);

    const descriptions = new Set();
    for (const card of results.querySelectorAll('.quote-card')) {
      const button = card.querySelector('button');
      expect(button.disabled).toBe(true);
      expect(button.textContent).toBe('Accept Cover — unavailable');
      const descriptionId = button.getAttribute('aria-describedby');
      descriptions.add(descriptionId);
      expect(card.querySelector(`#${descriptionId}`).textContent)
        .toBe('Cover acceptance is not available yet. You can compare quotes only.');
      const onClick = vi.fn();
      button.addEventListener('click', onClick);
      button.click();
      expect(onClick).not.toHaveBeenCalled();
    }
    expect(descriptions.size).toBe(2);
  });

  it('renders fallback values for incomplete quote results', () => {
    const results = document.createElement('div');

    renderQuotes(results, [{ premiumAmount: 0 }]);

    expect(results.textContent).toContain('Unknown underwriter');
    expect(results.textContent).toContain('Not supplied');
    expect(results.textContent).toContain('Unavailable');
    expect(results.textContent).toContain('£0.00');
  });

  it('renders an empty state when no quotes are returned', () => {
    const results = document.createElement('div');

    renderQuotes(results, []);

    expect(results.className).toBe('empty-state');
    expect(results.textContent).toBe('No underwriters returned a quote for this risk.');
  });

  it('renders quote text without interpreting markup', () => {
    const results = document.createElement('div');

    renderQuotes(results, [
      {
        underwriterName: '<script>alert("x")</script>',
        premiumAmount: 120,
        riskRating: '<b>Low</b>',
        region: '<img src=x>',
      },
    ]);

    expect(results.querySelector('script')).toBeNull();
    expect(results.querySelector('img')).toBeNull();
    expect(results.textContent).toContain('<script>alert("x")</script>');
    expect(results.textContent).toContain('<b>Low</b>');
  });

  it('submits quote requests to the gateway and renders success', async () => {
    const { form, button, status, results } = createDomHarness();
    const fetchQuotes = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({
        quotes: [
          {
            underwriterName: 'AvivaScheme',
            premiumAmount: 210,
            riskRating: 'Medium',
            region: 'London',
          },
        ],
      }),
    });

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);

    expect(fetchQuotes).toHaveBeenCalledWith('/api/quotes', expect.objectContaining({
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
    }));
    expect(JSON.parse(fetchQuotes.mock.calls[0][1].body)).toMatchObject({
      client: { firstName: 'Jane' },
      property: { isUnoccupied: false },
    });
    expect(button.disabled).toBe(false);
    expect(button.textContent).toBe('Generate quotes');
    expect(status.textContent).toBe('Quotes returned in premium order.');
    expect(results.textContent).toContain('AvivaScheme');
  });

  it('shows loading state while quote request is pending', async () => {
    const { form, button, status, results } = createDomHarness();
    const pendingResponse = createDeferredResponse();
    const fetchQuotes = vi.fn().mockReturnValue(pendingResponse.promise);

    const submitPromise = handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);

    expect(button.disabled).toBe(true);
    expect(button.textContent).toBe('Generating...');
    expect(status.textContent).toBe('Requesting quotes from underwriters...');
    expect(results.textContent).toBe('Quote request is being processed.');

    pendingResponse.resolve({
      ok: true,
      json: async () => ({ quotes: [] }),
    });
    await submitPromise;
  });

  it.each([true, false])('ignores overlapping submissions and permits retry after ok=%s', async (ok) => {
    const { form, button, status, results } = createDomHarness();
    const pending = createDeferredResponse();
    const fetchQuotes = vi.fn().mockReturnValueOnce(pending.promise).mockResolvedValue({
      ok: true,
      json: async () => ({ quotes: [] }),
    });

    const first = handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);
    const duplicateEvent = fakeSubmitEvent();
    await handleQuoteSubmit(duplicateEvent, form, button, status, results, fetchQuotes);

    expect(duplicateEvent.preventDefault).toHaveBeenCalledOnce();
    expect(fetchQuotes).toHaveBeenCalledOnce();
    expect(button.disabled).toBe(true);
    expect(results.textContent).toBe('Quote request is being processed.');
    pending.resolve({ ok, json: async () => ok ? { quotes: [] } : { error: 'Gateway failed.' } });
    await first;
    expect(button.disabled).toBe(false);

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);
    expect(fetchQuotes).toHaveBeenCalledTimes(2);
  });

  it('allows separate forms to submit independently', async () => {
    const first = createDomHarness();
    const second = createDomHarness();
    const pending = createDeferredResponse();
    const fetchQuotes = vi.fn().mockReturnValue(pending.promise);

    const firstSubmit = handleQuoteSubmit(fakeSubmitEvent(), first.form, first.button, first.status, first.results, fetchQuotes);
    const secondSubmit = handleQuoteSubmit(fakeSubmitEvent(), second.form, second.button, second.status, second.results, fetchQuotes);

    expect(fetchQuotes).toHaveBeenCalledTimes(2);
    pending.resolve({ ok: true, json: async () => ({ quotes: [] }) });
    await Promise.all([firstSubmit, secondSubmit]);
  });

  it('binds the form submit event to quote submission', async () => {
    const { form, button, status, results } = createDomHarness();
    const fetchQuotes = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ quotes: [] }),
    });

    bindQuoteForm(form, button, status, results, fetchQuotes);
    form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    await Promise.resolve();

    expect(fetchQuotes).toHaveBeenCalledOnce();
  });

  it('displays API validation errors', async () => {
    const { form, button, status, results } = createDomHarness();
    const fetchQuotes = vi.fn().mockResolvedValue({
      ok: false,
      json: async () => ({ errors: ['A valid client email is required.'] }),
    });

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);

    expect(status.classList.contains('error')).toBe(true);
    expect(status.textContent).toBe('A valid client email is required.');
    expect(results.textContent).toBe('No quotes to display yet.');
  });

  it('displays network failures', async () => {
    const { form, button, status, results } = createDomHarness();
    const fetchQuotes = vi.fn().mockRejectedValue(new Error('Network unavailable'));

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);

    expect(status.classList.contains('error')).toBe(true);
    expect(status.textContent).toBe('Network unavailable');
    expect(results.textContent).toBe('No quotes to display yet.');
  });

  it.each([
    ['HTML error', false, '<html>Bad gateway</html>', 'Quote request failed. Please try again.'],
    ['empty error', false, '', 'Quote request failed. Please try again.'],
    ['malformed success', true, '{broken', 'Gateway returned an invalid response. Please try again.'],
    ['empty success', true, '', 'Gateway returned an invalid response. Please try again.'],
  ])('displays a readable message for %s responses', async (_label, ok, body, message) => {
    const { form, button, status, results } = createDomHarness();
    const fetchQuotes = vi.fn().mockResolvedValue({
      ok,
      json: async () => JSON.parse(body),
    });

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, fetchQuotes);

    expect(status.classList.contains('error')).toBe(true);
    expect(status.textContent).toBe(message);
    expect(results.textContent).toBe('No quotes to display yet.');
    expect(button.disabled).toBe(false);
  });

  it('clears previous error state after a later successful request', async () => {
    const { form, button, status, results } = createDomHarness();
    const failedRequest = vi.fn().mockRejectedValue(new Error('Network unavailable'));
    const successfulRequest = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ quotes: [] }),
    });

    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, failedRequest);
    await handleQuoteSubmit(fakeSubmitEvent(), form, button, status, results, successfulRequest);

    expect(status.classList.contains('error')).toBe(false);
    expect(status.textContent).toBe('Quotes returned in premium order.');
  });

  it('formats helper output safely', () => {
    expect(formatCurrency(120)).toBe('£120.00');
    expect(formatError({ error: 'Gateway failed.' })).toBe('Gateway failed.');
    expect(formatError({ errors: ['First', 'Second'] })).toBe('First Second');
    expect(formatError({})).toBe('Quote request failed.');
  });

  it('uses generic error message when validation error list is empty', () => {
    expect(formatError({ errors: [] })).toBe('Quote request failed.');
  });
});

function createDomHarness() {
  const form = createForm();
  const button = document.createElement('button');
  button.textContent = 'Generate quotes';
  const status = document.createElement('p');
  const results = document.createElement('div');

  return { form, button, status, results };
}

function createForm() {
  const form = document.createElement('form');
  form.innerHTML = `
    <input name="firstName" value="Jane">
    <input name="lastName" value="Broker">
    <input name="email" value="jane@example.com">
    <input name="postcode" value="SW1A 1AA">
    <input name="yearBuilt" value="1910">
    <input name="rebuildCost" value="750000">
    <input name="isUnoccupied" type="checkbox">
  `;

  return form;
}

function fakeSubmitEvent() {
  return {
    preventDefault: vi.fn(),
  };
}

function createDeferredResponse() {
  let resolve;
  const promise = new Promise((promiseResolve) => {
    resolve = promiseResolve;
  });

  return { promise, resolve };
}
