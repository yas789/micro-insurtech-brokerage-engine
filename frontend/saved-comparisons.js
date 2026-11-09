export function normalizeReference(value) {
  if (typeof value !== 'string'
    || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
    || value === '00000000-0000-0000-0000-000000000000') return null;
  return value.toLowerCase();
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
