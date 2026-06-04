import { authEnabled, loginRequest } from './authConfig.js'

// Returns an Authorization header for outbound API calls when a user is signed
// in, or {} when auth is disabled / no token is available. The msal-browser
// module is only imported when auth is enabled, so it stays out of the test
// and mock-mode code paths.
export async function getAuthHeaders() {
  if (!authEnabled) return {}
  const { msalInstance } = await import('./msalInstance.js')
  if (!msalInstance) return {}

  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0]
  if (!account) return {}

  try {
    const result = await msalInstance.acquireTokenSilent({ ...loginRequest, account })
    return { Authorization: `Bearer ${result.accessToken}` }
  } catch {
    // Silent acquisition can fail (e.g. expired session). Don't block the call;
    // the backend simply receives no token.
    return {}
  }
}
