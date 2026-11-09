import { comparisonMetadata } from './saved-comparisons.js';

export const historyStorageKey = 'micro-insurtech.comparisons.v1';
const limit = 20;

export function renderComparisonHistory(container, entries, open, actions = {}) {
  container.replaceChildren();
  container.tabIndex = -1;
  if (!entries.length) {
    container.textContent = 'No recent comparisons on this browser.';
    return;
  }
  const list = document.createElement('ul');
  if (actions.clear) {
    const clear = document.createElement('button');
    clear.type = 'button';
    clear.textContent = 'Clear browser history';
    clear.addEventListener('click', () => { actions.clear(); container.focus(); });
    container.append(clear);
  }
  for (const entry of entries) {
    const row = document.createElement('li');
    const link = document.createElement('a');
    link.href = `/#quote=${entry.quoteReference}`;
    link.textContent = `Open comparison ${entry.quoteReference}`;
    link.addEventListener('click', (event) => { event.preventDefault(); open(entry.quoteReference); });
    const time = document.createElement('time');
    time.dateTime = entry.createdAt;
    time.textContent = new Date(entry.createdAt).toLocaleString('en-GB');
    row.append(link, document.createTextNode(' — '), time);
    if (actions.remove) {
      const remove = document.createElement('button');
      remove.type = 'button';
      remove.textContent = 'Remove';
      remove.setAttribute('aria-label', `Remove comparison ${entry.quoteReference} from this browser`);
      remove.addEventListener('click', () => {
        actions.remove(entry.quoteReference);
        (container.querySelector('a') || container).focus();
      });
      row.append(remove);
    }
    list.append(row);
  }
  container.append(list);
}

function normalizeEntries(entries) {
  if (!Array.isArray(entries)) return [];
  const unique = new Map();
  entries.map(comparisonMetadata).filter(Boolean)
    .sort((left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt))
    .forEach((entry) => { if (!unique.has(entry.quoteReference)) unique.set(entry.quoteReference, entry); });
  return [...unique.values()].slice(0, limit);
}

export function createComparisonHistory(storageProvider = () => window.localStorage) {
  let storage;
  let entries = [];
  try {
    storage = storageProvider();
    const saved = JSON.parse(storage.getItem(historyStorageKey));
    if (saved?.version === 1) entries = normalizeEntries(saved.entries);
  } catch { /* Storage is optional; keep this page's history in memory. */ }

  const persist = () => {
    try { storage?.setItem(historyStorageKey, JSON.stringify({ version: 1, entries })); } catch { /* Quota or privacy mode. */ }
  };
  return {
    list: () => entries.map((entry) => ({ ...entry })),
    remember(comparison) {
      const metadata = comparisonMetadata(comparison);
      if (!metadata) return;
      entries = normalizeEntries([metadata, ...entries]);
      persist();
    },
    remove(reference) {
      entries = entries.filter((entry) => entry.quoteReference !== reference);
      persist();
    },
    clear() {
      entries = [];
      persist();
    },
  };
}
