import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

// so this is the entry point for our actual app
// it's job is to attach the React component tree (so, the nested foramtion of components or the tangled mess of components, depends on YOU)
// to the browser's DOM