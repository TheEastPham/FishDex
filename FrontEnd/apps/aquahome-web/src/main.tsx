import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '@fishlover/shared/i18n'; // initialize i18n before render
import 'flag-icons/css/flag-icons.min.css';
import './index.css';
import App from './App';

console.info(`[FishLover] FE version: ${import.meta.env.VITE_APP_VERSION ?? 'dev'}`);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>
);
