import { bindQuoteForm } from './quote-ui.js';
import { bindSavedComparisonLinks, renderComparisonMetadata } from './saved-comparisons.js';
import { createComparisonHistory, renderComparisonHistory } from './comparison-history.js';

const quoteForm = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const quoteResults = document.querySelector('#quote-results');

if (quoteForm && submitButton && statusMessage && quoteResults) {
  const metadata = document.querySelector('#comparison-details');
  const summary = document.querySelector('#saved-property-summary');
  const recent = document.querySelector('#comparison-history');
  const history = createComparisonHistory();
  const historyStatus = document.querySelector('#history-status');
  let links;
  const refreshHistory = () => {
    if (recent) renderComparisonHistory(recent, history.list(), (reference) => links.navigate(reference), {
      remove(reference) {
        history.remove(reference);
        refreshHistory();
        if (historyStatus) historyStatus.textContent = 'Removed from this browser. The saved comparison and private link still exist.';
      },
      clear() {
        history.clear();
        refreshHistory();
        if (historyStatus) historyStatus.textContent = 'Browser history cleared. Saved comparisons and private links still exist.';
      },
    });
  };
  const remember = (comparison) => {
    history.remember(comparison);
    refreshHistory();
  };
  quoteForm.addEventListener('comparison-pending', () => {
    if (window.location.hash.startsWith('#quote=')) {
      window.history.replaceState(null, '', window.location.pathname + window.location.search);
    }
    metadata?.replaceChildren();
    summary?.replaceChildren();
  });
  quoteForm.addEventListener('comparison-created', (event) => {
    if (metadata) renderComparisonMetadata(metadata, event.detail);
    remember(event.detail);
    document.querySelector('#results-title')?.focus();
  });
  bindQuoteForm(quoteForm, submitButton, statusMessage, quoteResults);
  if (metadata && summary) links = bindSavedComparisonLinks({ status: statusMessage, results: quoteResults, metadata, summary,
    onOpened(comparison) {
      remember(comparison);
      document.querySelector('#results-title')?.focus();
    },
  });
  refreshHistory();
}
