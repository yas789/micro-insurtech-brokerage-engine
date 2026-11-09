import { formatCurrency, formatError, renderEmptyState, renderQuotes, setStatus } from './quote-ui.js';

export function normalizeReference(value) {
  if (typeof value !== 'string'
    || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
    || value === '00000000-0000-0000-0000-000000000000') return null;
  return value.toLowerCase();
}

export async function loadSavedComparison(reference, elements, request = fetch) {
  const { status, results, metadata, summary } = elements;
  metadata.replaceChildren();
  summary.replaceChildren();
  const normalized = normalizeReference(reference);
  if (!normalized) {
    renderEmptyState(results, 'No saved comparison to display.');
    setStatus(status, 'Comparison link is invalid.', true);
    return null;
  }
  results.setAttribute('aria-busy', 'true');
  renderEmptyState(results, 'Loading saved comparison...');
  setStatus(status, 'Loading saved comparison...');
  try {
    const response = await request(`/api/quotes/${normalized}`, { method: 'GET', headers: { Accept: 'application/json' }, cache: 'no-store' });
    let body;
    try { body = await response.json(); } catch { throw new Error('Saved comparison returned an invalid response. Please try again.'); }
    if (!response.ok) throw new Error(response.status === 404 ? 'Saved comparison was not found.' : formatError(body));
    if (comparisonMetadata(body)?.quoteReference !== normalized || !Array.isArray(body.quotes)
      || typeof body.property?.postcode !== 'string') throw new Error('Saved comparison returned an invalid response. Please try again.');
    renderComparisonMetadata(metadata, body);
    renderPropertySummary(summary, body.property);
    renderQuotes(results, body.quotes);
    setStatus(status, body.quotes.length ? 'Saved comparison loaded. Prices are the original saved snapshot.' : 'No underwriters returned a quote for this risk.');
    return body;
  } catch (error) {
    renderEmptyState(results, 'No saved comparison to display.');
    setStatus(status, error instanceof Error ? error.message : 'Saved comparison could not be loaded.', true);
    return null;
  } finally {
    results.setAttribute('aria-busy', 'false');
  }
}

export function renderPropertySummary(container, property) {
  container.textContent = `Saved property: ${property.postcode}; built ${property.yearBuilt}; rebuild cost ${formatCurrency(property.rebuildCost)}; ${property.isUnoccupied ? 'unoccupied' : 'occupied'}; region ${property.region || 'Unavailable'}.`;
}

export function bindSavedComparisonLinks(elements, request = fetch, location = window.location, events = window) {
  const open = (reference) => loadSavedComparison(reference, elements, request);
  const loadFragment = () => {
    if (location.hash.startsWith('#quote=')) return open(location.hash.slice(7));
    return Promise.resolve(null);
  };
  events.addEventListener('hashchange', loadFragment);
  loadFragment();
  return { open, loadFragment };
}

export function comparisonMetadata(value) {
  const reference = normalizeReference(value?.quoteReference);
  if (!reference || typeof value?.createdAt !== 'string' || !Number.isFinite(Date.parse(value.createdAt))) return null;
  return { quoteReference: reference, createdAt: new Date(value.createdAt).toISOString() };
}

export function renderComparisonMetadata(container, comparison) {
  container.replaceChildren();
  const metadata = comparisonMetadata(comparison);
  if (!metadata) return;
  const reference = document.createElement('p');
  reference.textContent = `Saved comparison: ${metadata.quoteReference}`;
  const time = document.createElement('time');
  time.dateTime = metadata.createdAt;
  time.textContent = `Created ${new Date(metadata.createdAt).toLocaleString('en-GB')}`;
  const link = document.createElement('a');
  link.href = `/#quote=${metadata.quoteReference}`;
  link.textContent = 'Open private comparison link';
  const explanation = document.createElement('p');
  explanation.textContent = 'Anyone with this link can view the property details and saved prices. Prices are a saved snapshot; cover acceptance is unavailable.';
  container.append(reference, time, document.createElement('br'), link, explanation);
}
