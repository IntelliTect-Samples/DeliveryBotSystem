import { useEffect } from 'react'
import { useMsal, useIsAuthenticated } from '@azure/msal-react'
import { authEnabled, loginRequest, ADMIN_GROUP_ID } from './authConfig.js'

// Wraps the app. When auth is disabled, renders children as-is. When enabled,
// requires an interactive sign-in and (if a group is configured) membership in
// the DeliveryBot-Admin group before showing the app.
export default function AuthGate({ children }) {
  if (!authEnabled) return children
  return <GatedContent>{children}</GatedContent>
}

function GatedContent({ children }) {
  const { instance, accounts } = useMsal()
  const isAuthenticated = useIsAuthenticated()

  useEffect(() => {
    if (!isAuthenticated) {
      instance.loginRedirect(loginRequest).catch((err) => console.error(err))
    }
  }, [isAuthenticated, instance])

  if (!isAuthenticated) {
    return <Centered title="Signing in…" body="Redirecting you to staff sign-in." />
  }

  const account = accounts[0]
  const groups = account?.idTokenClaims?.groups ?? []
  const inAdminGroup = !ADMIN_GROUP_ID || groups.includes(ADMIN_GROUP_ID)

  if (!inAdminGroup) {
    return (
      <Centered
        title="Access denied"
        body="Your account is not a member of the DeliveryBot-Admin group."
        onSignOut={() => instance.logoutRedirect()}
      />
    )
  }

  return children
}

function Centered({ title, body, onSignOut }) {
  return (
    <div style={styles.wrap}>
      <div style={styles.card}>
        <span style={styles.mark}>🤖</span>
        <h1 style={styles.title}>{title}</h1>
        <p style={styles.body}>{body}</p>
        {onSignOut && (
          <button style={styles.btn} onClick={onSignOut}>
            Sign out
          </button>
        )}
      </div>
    </div>
  )
}

const styles = {
  wrap: {
    minHeight: '100vh',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    padding: '2rem',
  },
  card: {
    textAlign: 'center',
    maxWidth: '24rem',
    padding: '2.5rem',
    border: '1px solid var(--border)',
    borderRadius: '16px',
    background: 'var(--bg-elev)',
  },
  mark: { fontSize: '2.5rem' },
  title: { margin: '0.75rem 0 0.5rem', fontSize: '1.5rem' },
  body: { margin: 0, color: 'var(--text-dim)' },
  btn: {
    marginTop: '1.25rem',
    background: 'var(--accent)',
    color: 'white',
    border: 'none',
    padding: '0.5rem 1rem',
    borderRadius: '8px',
    fontSize: '0.9rem',
  },
}
