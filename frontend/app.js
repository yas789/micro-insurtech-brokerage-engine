import { bindQuoteForm } from './quote-ui.js';
import { renderComparisonMetadata } from './saved-comparisons.js';

const quoteForm = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const quoteResults = document.querySelector('#quote-results');

if (quoteForm && submitButton && statusMessage && quoteResults) {
  const metadata = document.querySelector('#comparison-details');
  quoteForm.addEventListener('comparison-pending', () => metadata?.replaceChildren());
  quoteForm.addEventListener('comparison-created', (event) => {
    if (metadata) renderComparisonMetadata(metadata, event.detail);
  });
  bindQuoteForm(quoteForm, submitButton, statusMessage, quoteResults);
}
