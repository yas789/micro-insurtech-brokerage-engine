export function bindQuoteForm(formElement, buttonElement, statusElement, resultsElement, quoteRequest = fetch) {
  formElement.addEventListener('submit', async (event) => {
    await handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, quoteRequest);
  });
}

export async function handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, quoteRequest = fetch) {
  event.preventDefault();

  const requestPayload = buildQuotePayload(new FormData(formElement));
  setLoading(buttonElement, true);
  setStatus(statusElement, 'Requesting quotes from underwriters...');
  renderEmptyState(resultsElement, 'Quote request is being processed.');

  try {
    const response = await quoteRequest('/api/quotes', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json',
      },
      body: JSON.stringify(requestPayload),
    });
    const responseBody = await response.json();

    if (!response.ok) {
      throw new Error(formatError(responseBody));
    }

    renderQuotes(resultsElement, responseBody.quotes || []);
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
    resultsElement.append(createQuoteCard(quote, index));
  });
}

export function createQuoteCard(quote, index) {
  const card = document.createElement('article');
  card.className = 'quote-card';

  const rank = document.createElement('div');
  rank.className = 'rank';
  rank.textContent = `Option ${index + 1}`;

  const title = document.createElement('h3');
  title.textContent = quote.underwriterName || 'Unknown underwriter';

  const premium = document.createElement('p');
  premium.className = 'premium';
  premium.textContent = formatCurrency(quote.premiumAmount);

  const details = document.createElement('dl');
  details.append(
    createQuoteDetail('Risk rating', quote.riskRating || 'Not supplied'),
    createQuoteDetail('Region', quote.region || 'Unavailable'),
  );

  const acceptButton = document.createElement('button');
  acceptButton.type = 'button';
  acceptButton.className = 'accept-button';
  acceptButton.textContent = 'Accept Cover';

  card.append(rank, title, premium, details, acceptButton);

  return card;
}

function createQuoteDetail(label, value) {
  const row = document.createElement('div');
  const term = document.createElement('dt');
  const description = document.createElement('dd');

  term.textContent = label;
  description.textContent = value;
  row.append(term, description);

  return row;
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
