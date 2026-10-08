import './app/styles/fonts.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { App } from './app/App';
import { appConfig } from './core/config';

document.title = appConfig.name;

const container = document.getElementById('root');
if (!container) throw new Error('Root element #root not found.');

createRoot(container).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
