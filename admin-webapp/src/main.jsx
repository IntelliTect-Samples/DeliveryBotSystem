import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import { EventType } from '@azure/msal-browser'
import './index.css'
import App from './App.jsx'
import { authEnabled } from './auth/authConfig.js'
import { msalInstance } from './auth/msalInstance.js'
import { initTelemetry } from './telemetry/appInsights.js'

// Azure Monitor: start client telemetry (no-op when not configured).
initTelemetry()

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
  // MSAL v5 requires initialize() before use. MsalProvider itself handles the
  // returning redirect response — we must NOT also call handleRedirectPromise
  // here, or the two race and the sign-in loops. We just keep the active
  // account in sync from the cache and from successful logins.
  msalInstance
    .initialize()
    .then(() => {
      const accounts = msalInstance.getAllAccounts()
      if (accounts.length > 0) {
        msalInstance.setActiveAccount(accounts[0])
      }
      msalInstance.addEventCallback((event) => {
        if (event.eventType === EventType.LOGIN_SUCCESS && event.payload?.account) {
          msalInstance.setActiveAccount(event.payload.account)
        }
      })
      render()
    })
    .catch((err) => {
      console.error(err)
      render()
    })
} else {
  render()
}
