import { describe, expect, it, vi } from 'vitest';
import { bindSavedComparisonLinks, comparisonMetadata, loadSavedComparison, renderComparisonMetadata } from '../saved-comparisons.js';

export const reference = '3fc8045c-0a9a-4df4-96d6-f530bddb899a';
export const saved = { quoteReference: reference, createdAt: '2026-11-08T12:00:00Z', quotes: [],
  property: { postcode: 'SW1A 1AA', yearBuilt: 1910, rebuildCost: 750000, isUnoccupied: false, region: 'London' } };

function elements() {
  return Object.fromEntries(['status', 'results', 'metadata', 'summary'].map((key) => [key, document.createElement('div')]));
}

describe('opening saved comparisons', () => {
  it('uses GET and displays the saved snapshot without client fields', async () => {
    const ui = elements();
    const request = vi.fn().mockResolvedValue({ ok: true, json: async () => saved });
    expect(await loadSavedComparison(reference, ui, request)).toEqual(saved);
    expect(request).toHaveBeenCalledWith(`/api/quotes/${reference}`, expect.objectContaining({ method: 'GET', cache: 'no-store' }));
    expect(ui.summary.textContent).toContain('SW1A 1AA');
    expect(ui.results.getAttribute('aria-busy')).toBe('false');
  });

  it('opens a fragment on startup and on navigation', async () => {
    const request = vi.fn().mockResolvedValue({ ok: true, json: async () => saved });
    const events = new EventTarget();
    const location = { hash: `#quote=${reference}` };
    const links = bindSavedComparisonLinks(elements(), request, location, events);
    await links.loadFragment();
    events.dispatchEvent(new Event('hashchange'));
    expect(request).toHaveBeenCalledTimes(3);
  });

  it('rejects malformed references without contacting the gateway', async () => {
    const request = vi.fn();
    const ui = elements();
    await loadSavedComparison('not-a-uuid', ui, request);
    expect(request).not.toHaveBeenCalled();
    expect(ui.status.textContent).toBe('Comparison link is invalid.');
  });

  it.each([
    [{ ok: false, status: 404, json: async () => ({}) }, 'not found'],
    [{ ok: false, status: 502, json: async () => ({ error: 'Core engine is unavailable.' }) }, 'unavailable'],
    [{ ok: true, json: async () => null }, 'invalid response'],
  ])('handles unsuccessful or unusable responses', async (response, message) => {
    const ui = elements();
    expect(await loadSavedComparison(reference, ui, async () => response)).toBeNull();
    expect(ui.status.textContent).toContain(message);
    expect(ui.metadata.childNodes).toHaveLength(0);
    expect(ui.results.getAttribute('aria-busy')).toBe('false');
  });
});

describe('saved comparison metadata', () => {
  it('renders a private fragment link and honest access explanation without client identity', () => {
    const container = document.createElement('div');
    renderComparisonMetadata(container, { ...saved, client: { email: 'private@example.com' } });
    expect(container.querySelector('a').getAttribute('href')).toBe(`/#quote=${reference}`);
    expect(container.textContent).toContain('Anyone with this link');
    expect(container.textContent).toContain('snapshot');
    expect(container.querySelector('time').dateTime).toBe('2026-11-08T12:00:00.000Z');
    expect(container.textContent).not.toContain('private@example.com');
  });

  it('clears old metadata for unusable responses', () => {
    const container = document.createElement('div');
    renderComparisonMetadata(container, saved);
    renderComparisonMetadata(container, { quotes: [] });
    expect(container.childNodes).toHaveLength(0);
    expect(comparisonMetadata({ ...saved, quoteReference: '<script>' })).toBeNull();
    expect(comparisonMetadata({ ...saved, createdAt: 'broken' })).toBeNull();
  });
});
