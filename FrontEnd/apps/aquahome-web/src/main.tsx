import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@fishlover/shared/i18n'; // initialize i18n before render
import './index.css';
import App from './App';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
