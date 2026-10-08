const requestingQuotesMessage = 'Requesting quotes from underwriters...';
const processingQuotesMessage = 'Quote request is being processed.';
const quotesReturnedMessage = 'Quotes returned in premium order.';
const noQuotesMessage = 'No quotes to display yet.';
const quoteApiPath = '/api/quotes';
const pendingQuoteForms = new WeakSet();
const jsonHeaders = {
  'Content-Type': 'application/json',
  Accept: 'application/json',
};

export function bindQuoteForm(formElement, buttonElement, statusElement, resultsElement, quoteRequest = fetch) {
  formElement.addEventListener('submit', async (event) => {
    await handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, quoteRequest);
  });
}

export async function handleQuoteSubmit(event, formElement, buttonElement, statusElement, resultsElement, quoteRequest = fetch) {
  event.preventDefault();

  if (pendingQuoteForms.has(formElement)) {
    return;
  }

  const requestPayload = buildQuotePayload(new FormData(formElement));
  pendingQuoteForms.add(formElement);
  setLoading(buttonElement, true);
  setStatus(statusElement, requestingQuotesMessage);
  renderEmptyState(resultsElement, processingQuotesMessage);

  try {
    const response = await quoteRequest(quoteApiPath, {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify(requestPayload),
    });
    let responseBody;
    try {
      responseBody = await response.json();
    } catch {
      throw new Error(response.ok
        ? 'Gateway returned an invalid response. Please try again.'
        : 'Quote request failed. Please try again.');
    }

    if (!response.ok) {
      throw new Error(formatError(responseBody));
    }

    renderQuotes(resultsElement, responseBody.quotes || []);
    setStatus(statusElement, quotesReturnedMessage);
  } catch (error) {
    renderEmptyState(resultsElement, noQuotesMessage);
    setStatus(statusElement, error instanceof Error ? error.message : 'Quote request failed.', true);
  } finally {
    pendingQuoteForms.delete(formElement);
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
  acceptButton.textContent = 'Accept Cover — unavailable';
  acceptButton.disabled = true;

  const acceptanceNotice = document.createElement('p');
  acceptanceNotice.id = `cover-unavailable-${index}`;
  acceptanceNotice.className = 'acceptance-notice';
  acceptanceNotice.textContent = 'Cover acceptance is not available yet. You can compare quotes only.';
  acceptButton.setAttribute('aria-describedby', acceptanceNotice.id);

  card.append(rank, title, premium, details, acceptButton, acceptanceNotice);

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
  if (Array.isArray(body?.errors) && body.errors.length > 0) {
    return body.errors.join(' ');
  }

  return body?.error || 'Quote request failed.';
}
