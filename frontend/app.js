import { bindQuoteForm } from './quote-ui.js';

const quoteForm = document.querySelector('#quote-form');
const submitButton = document.querySelector('#submit-button');
const statusMessage = document.querySelector('#form-status');
const quoteResults = document.querySelector('#quote-results');

if (quoteForm && submitButton && statusMessage && quoteResults) {
  bindQuoteForm(quoteForm, submitButton, statusMessage, quoteResults);
}
