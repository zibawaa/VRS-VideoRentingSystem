import { StrictMode, useState, useEffect } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import Admin from './Admin.tsx'

// lightweight hash-based router that switches between Marketplace and Admin views
function Root() {
  const [route, setRoute] = useState(window.location.hash);

  // listen for hash changes so in-app navigation re-renders the correct view
  useEffect(() => {
    const onHash = () => setRoute(window.location.hash);
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  // #/admin loads the admin panel, everything else loads the marketplace
  if (route === '#/admin') return <Admin />;
  return <App />;
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Root />
  </StrictMode>,
)
