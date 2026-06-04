import { useMsal } from '@azure/msal-react'

// Shows the signed-in staff member's name and a sign-out control in the top
// nav. Only rendered when auth is enabled (inside the MsalProvider).
export default function UserMenu() {
  const { instance, accounts } = useMsal()
  const account = accounts[0]
  const name = account?.name ?? account?.username ?? 'Signed in'

  return (
    <div style={styles.wrap}>
      <span style={styles.name} title={account?.username}>
        {name}
      </span>
      <button style={styles.btn} onClick={() => instance.logoutRedirect()}>
        Sign out
      </button>
    </div>
  )
}

const styles = {
  wrap: { display: 'flex', alignItems: 'center', gap: '0.6rem' },
  name: { color: 'var(--text)', fontSize: '0.9rem', fontWeight: 500 },
  btn: {
    background: 'transparent',
    color: 'var(--text-dim)',
    border: '1px solid var(--border)',
    padding: '0.4rem 0.75rem',
    borderRadius: '8px',
    fontSize: '0.85rem',
  },
}
