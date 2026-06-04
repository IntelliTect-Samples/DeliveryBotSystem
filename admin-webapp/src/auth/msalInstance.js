import { PublicClientApplication } from '@azure/msal-browser'
import { authEnabled, msalConfig } from './authConfig.js'

// A real MSAL instance is only constructed when auth is configured. While
// disabled this stays null and the app renders without an MsalProvider.
export const msalInstance = authEnabled ? new PublicClientApplication(msalConfig) : null
