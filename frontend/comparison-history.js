import { comparisonMetadata } from './saved-comparisons.js';

export const historyStorageKey = 'micro-insurtech.comparisons.v1';
const limit = 20;

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
  };
}
