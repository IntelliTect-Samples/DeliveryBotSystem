// Entra ID (Azure AD) configuration for staff sign-in (issue #54).
//
// Auth is gated behind env vars. While they're blank (e.g. before the Entra
// app registration exists) `authEnabled` is false and the admin app runs with
// auth disabled — so local dev and the current deployment keep working. Fill
// the VITE_ENTRA_* vars in (workflow env / .env.local) to switch auth on.

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID ?? ''
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID ?? ''

// Object ID of the DeliveryBot-Admin security group. When set, only members
// may use the app; when blank, any signed-in user is allowed.
export const ADMIN_GROUP_ID = import.meta.env.VITE_ENTRA_ADMIN_GROUP_ID ?? ''

// Auth turns on only when both the client and tenant IDs are present.
export const authEnabled = Boolean(clientId && tenantId)

const redirectUri =
  typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173'

export const msalConfig = {
  auth: {
    clientId,
    authority: `https://login.microsoftonline.com/${tenantId}`,
    redirectUri,
    postLogoutRedirectUri: redirectUri,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
}

// Keep the initial sign-in request to pure OIDC scopes. Requesting Graph scopes
// like User.Read here can force extra consent/admin policy checks even though
// the app only needs an ID token and group claims to gate staff access.
export const loginRequest = {
  scopes: ['openid', 'profile'],
}
