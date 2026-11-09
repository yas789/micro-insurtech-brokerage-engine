import { bindQuoteForm } from './quote-ui.js';
import { bindSavedComparisonLinks, renderComparisonMetadata } from './saved-comparisons.js';

const quoteForm = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const quoteResults = document.querySelector('#quote-results');

if (quoteForm && submitButton && statusMessage && quoteResults) {
  const metadata = document.querySelector('#comparison-details');
  const summary = document.querySelector('#saved-property-summary');
  quoteForm.addEventListener('comparison-pending', () => {
    if (window.location.hash.startsWith('#quote=')) {
      window.history.replaceState(null, '', window.location.pathname + window.location.search);
    }
    metadata?.replaceChildren();
    summary?.replaceChildren();
  });
  quoteForm.addEventListener('comparison-created', (event) => {
    if (metadata) renderComparisonMetadata(metadata, event.detail);
  });
  bindQuoteForm(quoteForm, submitButton, statusMessage, quoteResults);
  if (metadata && summary) bindSavedComparisonLinks({ status: statusMessage, results: quoteResults, metadata, summary });
}
