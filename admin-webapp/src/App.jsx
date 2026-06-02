import { useState } from 'react'
import BotsPage from './pages/BotsPage.jsx'
import OrdersPage from './pages/OrdersPage.jsx'

const tabs = [
  { id: 'bots', label: 'Bots' },
  { id: 'orders', label: 'Orders' },
  { id: 'config', label: 'Configuration' },
]

export default function App() {
  const [active, setActive] = useState('bots')

  return (
    <div style={styles.shell}>
      <nav style={styles.nav}>
        <div style={styles.brand}>
          <span style={styles.brandMark}>🤖</span>
          <div>
            <h2 style={styles.brandTitle}>DeliveryBot Admin</h2>
            <p style={styles.brandSub}>Issue #18 · WIP</p>
          </div>
        </div>
        <div style={styles.tabs}>
          {tabs.map((t) => (
            <button
              key={t.id}
              onClick={() => setActive(t.id)}
              style={{
                ...styles.tab,
                ...(active === t.id ? styles.tabActive : null),
              }}
            >
              {t.label}
            </button>
          ))}
        </div>
      </nav>

      <main style={styles.main}>
        {active === 'bots' && <BotsPage />}
        {active === 'orders' && <OrdersPage />}
        {active === 'config' && (
          <ComingSoon title="System Configuration" upstream="App Configuration Service" />
        )}
      </main>
    </div>
  )
}

function ComingSoon({ title, upstream }) {
  return (
    <section style={{ padding: '0 2rem 2rem' }}>
      <h1 style={{ margin: 0, fontSize: '1.75rem' }}>{title}</h1>
      <p style={{ color: 'var(--text-dim)' }}>
        Coming soon — depends on <strong>{upstream}</strong>.
      </p>
      <div
        style={{
          marginTop: '1rem',
          padding: '2rem',
          border: '1px dashed var(--border)',
          borderRadius: '12px',
          color: 'var(--text-dim)',
          textAlign: 'center',
        }}
      >
        Placeholder. This view will land once the upstream service is available.
      </div>
    </section>
  )
}

const styles = {
  shell: { minHeight: '100vh' },
  nav: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '1.25rem 2rem',
    borderBottom: '1px solid var(--border)',
    background: 'var(--bg-elev-2)',
    flexWrap: 'wrap',
    gap: '1rem',
  },
  brand: { display: 'flex', alignItems: 'center', gap: '0.75rem' },
  brandMark: { fontSize: '1.75rem' },
  brandTitle: { margin: 0, fontSize: '1.1rem' },
  brandSub: { margin: 0, color: 'var(--text-dim)', fontSize: '0.8rem' },
  tabs: { display: 'flex', gap: '0.25rem' },
  tab: {
    background: 'transparent',
    color: 'var(--text-dim)',
    border: '1px solid transparent',
    padding: '0.5rem 1rem',
    borderRadius: '8px',
    fontSize: '0.95rem',
  },
  tabActive: {
    color: 'var(--text)',
    background: 'var(--bg-elev)',
    border: '1px solid var(--border)',
  },
  main: { padding: '1.5rem 0' },
}
