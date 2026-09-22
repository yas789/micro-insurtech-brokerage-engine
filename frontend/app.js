const form = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const results = document.querySelector('#quote-results');

if (form && submitButton && statusMessage && results) {
  bindQuoteForm(form, submitButton, statusMessage, results);
}

export function bindQuoteForm(formElement, buttonElement, statusElement, resultsElement, fetchQuotes = fetch) {
  formElement.addEventListener('submit', async (event) => {
    await handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, fetchQuotes);
  });
}

export async function handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, fetchQuotes = fetch) {
  event.preventDefault();

  const payload = buildQuotePayload(new FormData(formElement));
  setLoading(buttonElement, true);
  setStatus(statusElement, 'Requesting quotes from underwriters...');
  renderEmptyState(resultsElement, 'Quote request is being processed.');

  try {
    const response = await fetchQuotes('/api/quotes', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      body: JSON.stringify(payload),
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(formatError(body));
    }

    renderQuotes(resultsElement, body.quotes || []);
    setStatus(statusElement, 'Quotes returned in premium order.');
  } catch (error) {
    renderEmptyState(resultsElement, 'No quotes to display yet.');
    setStatus(statusElement, error instanceof Error ? error.message : 'Quote request failed.', true);
  } finally {
    setLoading(buttonElement, false);
  }
}

export function buildQuotePayload(formData) {
  return {
    client: {
      firstName: String(formData.get('firstName') || '').trim(),
      lastName: String(formData.get('lastName') || '').trim(),
      email: String(formData.get('email') || '').trim(),
    },
    property: {
      postcode: String(formData.get('postcode') || '').trim(),
      yearBuilt: Number(formData.get('yearBuilt')),
      rebuildCost: Number(formData.get('rebuildCost')),
      isUnoccupied: formData.has('isUnoccupied'),
    },
  };
}

export function renderQuotes(resultsElement, quotes) {
  if (!Array.isArray(quotes) || quotes.length === 0) {
    renderEmptyState(resultsElement, 'No underwriters returned a quote for this risk.');
    return;
  }

  resultsElement.className = 'quote-grid';
  resultsElement.innerHTML = '';

  quotes.forEach((quote, index) => {
    const card = document.createElement('article');
    card.className = 'quote-card';

    card.innerHTML = `
      <div class="rank">Option ${index + 1}</div>
      <h3>${escapeHtml(quote.underwriterName || 'Unknown underwriter')}</h3>
      <p class="premium">${formatCurrency(quote.premiumAmount)}</p>
      <dl>
        <div>
          <dt>Risk rating</dt>
          <dd>${escapeHtml(quote.riskRating || 'Not supplied')}</dd>
        </div>
        <div>
          <dt>Region</dt>
          <dd>${escapeHtml(quote.region || 'Unavailable')}</dd>
        </div>
      </dl>
      <button type="button" class="accept-button">Accept Cover</button>
    `;

    resultsElement.append(card);
  });
}

export function renderEmptyState(resultsElement, message) {
  resultsElement.className = 'empty-state';
  resultsElement.textContent = message;
}

export function setLoading(buttonElement, isLoading) {
  buttonElement.disabled = isLoading;
  buttonElement.textContent = isLoading ? 'Generating...' : 'Generate quotes';
}

export function setStatus(statusElement, message, isError = false) {
  statusElement.textContent = message;
  statusElement.classList.toggle('error', isError);
}

export function formatCurrency(value) {
  return new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
  }).format(Number(value || 0));
}

export function formatError(body) {
  if (Array.isArray(body?.errors)) {
    return body.errors.join(' ');
  }

  return body?.error || 'Quote request failed.';
}

export function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}
