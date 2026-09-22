const form = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const results = document.querySelector('#quote-results');

form.addEventListener('submit', async (event) => {
  event.preventDefault();

  const payload = buildQuotePayload(new FormData(form));
  setLoading(true);
  setStatus('Requesting quotes from underwriters...');
  renderEmptyState('Quote request is being processed.');

  try {
    const response = await fetch('/api/quotes', {
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

    renderQuotes(body.quotes || []);
    setStatus('Quotes returned in premium order.');
  } catch (error) {
    renderEmptyState('No quotes to display yet.');
    setStatus(error instanceof Error ? error.message : 'Quote request failed.', true);
  } finally {
    setLoading(false);
  }
});

function buildQuotePayload(formData) {
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

function renderQuotes(quotes) {
  if (!Array.isArray(quotes) || quotes.length === 0) {
    renderEmptyState('No underwriters returned a quote for this risk.');
    return;
  }

  results.className = 'quote-grid';
  results.innerHTML = '';

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

    results.append(card);
  });
}

function renderEmptyState(message) {
  results.className = 'empty-state';
  results.textContent = message;
}

function setLoading(isLoading) {
  submitButton.disabled = isLoading;
  submitButton.textContent = isLoading ? 'Generating...' : 'Generate quotes';
}

function setStatus(message, isError = false) {
  statusMessage.textContent = message;
  statusMessage.classList.toggle('error', isError);
}

function formatCurrency(value) {
  return new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency: 'GBP',
  }).format(Number(value || 0));
}

function formatError(body) {
  if (Array.isArray(body?.errors)) {
    return body.errors.join(' ');
  }

  return body?.error || 'Quote request failed.';
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#039;');
}
