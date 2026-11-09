import { describe, expect, it } from 'vitest';
import { comparisonMetadata, renderComparisonMetadata } from '../saved-comparisons.js';

export const reference = '3fc8045c-0a9a-4df4-96d6-f530bddb899a';
export const saved = { quoteReference: reference, createdAt: '2026-11-08T12:00:00Z', quotes: [],
  property: { postcode: 'SW1A 1AA', yearBuilt: 1910, rebuildCost: 750000, isUnoccupied: false, region: 'London' } };

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
