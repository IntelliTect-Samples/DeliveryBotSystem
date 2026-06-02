import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import './index.css'
import App from './App.jsx'
import { authEnabled } from './auth/authConfig.js'
import { msalInstance } from './auth/msalInstance.js'

const root = createRoot(document.getElementById('root'))

function render() {
  root.render(
    <StrictMode>
      {authEnabled && msalInstance ? (
        <MsalProvider instance={msalInstance}>
          <App />
        </MsalProvider>
      ) : (
        <App />
      )}
    </StrictMode>,
  )
}

if (authEnabled && msalInstance) {
  // MSAL v5 requires initialize() before any auth call; handle a returning
  // redirect before first render so the account is available immediately.
  msalInstance
    .initialize()
    .then(() => msalInstance.handleRedirectPromise())
    .then((result) => {
      if (result?.account) msalInstance.setActiveAccount(result.account)
      const existing = msalInstance.getAllAccounts()
      if (!msalInstance.getActiveAccount() && existing.length > 0) {
        msalInstance.setActiveAccount(existing[0])
      }
      render()
    })
    .catch((err) => {
      console.error(err)
      render()
    })
} else {
  render()
}
