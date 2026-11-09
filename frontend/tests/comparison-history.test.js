import { describe, expect, it, vi } from 'vitest';
import { createComparisonHistory, historyStorageKey, renderComparisonHistory } from '../comparison-history.js';

const reference = '3fc8045c-0a9a-4df4-96d6-f530bddb899a';
const saved = { quoteReference: reference, createdAt: '2026-11-08T12:00:00Z' };
function storage(initial = null) {
  let value = initial;
  return { getItem: () => value, setItem: (_key, next) => { value = next; } };
}

describe('browser comparison history', () => {
  it('renders an empty state then reopens entries without exposing extra fields', () => {
    const container = document.createElement('div');
    const open = vi.fn();
    renderComparisonHistory(container, [], open);
    expect(container.textContent).toContain('No recent comparisons');
    renderComparisonHistory(container, [{ ...saved, email: 'private@example.com' }], open);
    container.querySelector('a').click();
    expect(open).toHaveBeenCalledWith(reference);
    expect(container.textContent).not.toContain('private@example.com');
    expect(container.querySelector('a').getAttribute('href')).toBe(`/#quote=${reference}`);
  });

  it('persists only normalized references and timestamps, deduplicated', () => {
    const store = storage();
    const history = createComparisonHistory(() => store);
    history.remember({ ...saved, client: { email: 'private@example.com' }, property: { postcode: 'private' } });
    history.remember({ ...saved, quoteReference: reference.toUpperCase() });
    expect(history.list()).toEqual([{ ...saved, createdAt: '2026-11-08T12:00:00.000Z' }]);
    expect(createComparisonHistory(() => store).list()).toEqual(history.list());
    expect(store.getItem(historyStorageKey)).not.toContain('private');
  });

  it('keeps the newest twenty and drops invalid entries', () => {
    const history = createComparisonHistory(() => storage());
    for (let day = 1; day <= 25; day += 1) history.remember({
      quoteReference: `${String(day).padStart(8, '0')}-0a9a-4df4-96d6-f530bddb899a`,
      createdAt: `2026-11-${String(day).padStart(2, '0')}T00:00:00Z`,
    });
    history.remember({ quoteReference: 'broken', createdAt: 'broken' });
    expect(history.list()).toHaveLength(20);
    expect(history.list()[0].createdAt).toContain('11-25');
    expect(history.list().at(-1).createdAt).toContain('11-06');
  });

  it.each(['broken JSON', '{"version":2,"entries":[]}', '{"version":1,"entries":{}}'])('tolerates corrupt/unsupported storage: %s', (initial) => {
    const history = createComparisonHistory(() => storage(initial));
    expect(history.list()).toEqual([]);
    history.remember(saved);
    expect(history.list()).toHaveLength(1);
  });

  it.each([
    () => { throw new Error('Access denied'); },
    () => ({ getItem() { throw new Error('Blocked'); }, setItem() { throw new Error('Quota'); } }),
  ])('continues with memory-only history when storage is blocked', (provider) => {
    const history = createComparisonHistory(provider);
    history.remember(saved);
    const copy = history.list();
    copy[0].quoteReference = 'modified';
    expect(history.list()[0].quoteReference).toBe(reference);
  });
});
